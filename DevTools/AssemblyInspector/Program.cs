using System.Reflection;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var options = ScanOptions.Parse(args);
if (!options.IsValid)
{
    Console.Error.WriteLine(options.ErrorMessage);
    WritePlaceholderReport(options, options.ErrorMessage);
    return 2;
}

if (!Directory.Exists(options.InputFolder))
{
    var message = $"Interop assemblies folder does not exist: {options.InputFolder}";
    Console.Error.WriteLine(message);
    WritePlaceholderReport(options, message);
    return 1;
}

var dllPaths = Directory.GetFiles(options.InputFolder, "*.dll", SearchOption.TopDirectoryOnly)
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (dllPaths.Length == 0)
{
    var message = $"No .dll files were found in: {options.InputFolder}";
    Console.Error.WriteLine(message);
    WritePlaceholderReport(options, message);
    return 1;
}

var scanner = new MetadataScanner(options);
var result = scanner.Scan(dllPaths);
ReportWriter.Write(options.OutputPath, options, result);

Console.WriteLine($"Scanned {result.AssembliesScanned} assembly file(s), {result.TypesScanned} type(s), {result.MembersScanned} member(s).");
Console.WriteLine($"Found {result.Candidates.Count} candidate(s).");
Console.WriteLine($"Wrote report: {Path.GetFullPath(options.OutputPath)}");
return result.Errors.Count == 0 ? 0 : 1;

static void WritePlaceholderReport(ScanOptions options, string? reason)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath)) ?? ".");
    File.WriteAllLines(options.OutputPath, ReportWriter.BuildPlaceholder(options, reason));
    Console.Error.WriteLine($"Wrote placeholder report: {Path.GetFullPath(options.OutputPath)}");
}

internal sealed class ScanOptions
{
    public string InputFolder { get; private init; } = string.Empty;
    public string OutputPath { get; private init; } = DefaultOutputPath();
    public int MaxResults { get; private init; } = 75;
    public bool IncludeLowConfidence { get; private init; }
    public bool Verbose { get; private init; }
    public bool IsValid { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ScanOptions Parse(string[] args)
    {
        string? input = null;
        var output = DefaultOutputPath();
        var maxResults = 75;
        var includeLow = false;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--output":
                    if (++i >= args.Length)
                    {
                        return Invalid("Missing value for --output.");
                    }

                    output = args[i];
                    break;
                case "--max-results":
                    if (++i >= args.Length || !int.TryParse(args[i], out maxResults) || maxResults < 1)
                    {
                        return Invalid("--max-results must be a positive integer.");
                    }

                    break;
                case "--include-low-confidence":
                    includeLow = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        return Invalid($"Unknown option: {arg}");
                    }

                    input ??= arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return Invalid("Usage: dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- <interop-assemblies-folder> [--output <path>] [--max-results <number>] [--include-low-confidence] [--verbose]");
        }

        return new ScanOptions
        {
            InputFolder = Path.GetFullPath(input),
            OutputPath = Path.GetFullPath(output),
            MaxResults = maxResults,
            IncludeLowConfidence = includeLow,
            Verbose = verbose,
            IsValid = true
        };
    }

    private static ScanOptions Invalid(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        OutputPath = DefaultOutputPath()
    };

    private static string DefaultOutputPath() => Path.Combine(FindRepoRoot(AppContext.BaseDirectory), "docs", "generated", "INTEROP_SCAN_REPORT.md");

    private static string FindRepoRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SOD_CityRelations.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

internal sealed class MetadataScanner
{
    private static readonly string[] Terms =
    {
        "Human", "Citizen", "Actor", "Interaction", "Interactable", "Dialogue", "Dialog", "Conversation",
        "Speech", "Talk", "Question", "Answer", "Witness", "Evidence", "Info", "Identity", "Name",
        "Address", "Workplace", "Seen", "Alibi", "Murder", "Case", "Crime", "CitizenBehaviour",
        "HumanBehaviour", "NewAI", "GameplayController", "InteractionController", "Player",
        "PlayerInteraction", "PlayerApartment", "SideJob", "Job", "CasePanel", "EvidenceWitness",
        "Telephone", "Message", "DDS", "DialogPreset", "Acquaintance", "Relationship", "Routine",
        "Location", "Company", "Residence"
    };

    private static readonly string[] StrongTypeTerms = { "Human", "Citizen", "NewAI", "Dialog", "Dialogue", "Conversation", "Interactable", "GameplayController", "InteractionController" };
    private static readonly string[] StrongMemberTerms = { "Talk", "Ask", "Question", "Answer", "Interact", "Speak", "Seen", "Witness", "Name", "Address", "Evidence" };
    private static readonly string[] DangerousTerms = { "Murder", "Case", "Crime", "Evidence", "Save", "Dialogue", "Dialog", "Answer", "Result" };
    private static readonly string[] ReadOnlyTerms = { "Get", "get_", "Is", "Has", "Can", "Find", "Check" };

    private readonly ScanOptions options;

    public MetadataScanner(ScanOptions options)
    {
        this.options = options;
    }

    public ScanResult Scan(IReadOnlyCollection<string> dllPaths)
    {
        var result = new ScanResult { AssembliesScanned = dllPaths.Count };

        foreach (var dllPath in dllPaths)
        {
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata)
                {
                    result.Warnings.Add($"Skipped non-.NET assembly metadata: {dllPath}");
                    continue;
                }

                var reader = peReader.GetMetadataReader();
                var assemblyName = GetAssemblyName(reader, dllPath);
                var likelyGameAssembly = IsLikelyGameAssembly(assemblyName);
                result.AssemblyNames.Add(assemblyName);
                if (likelyGameAssembly)
                {
                    result.LikelyGameAssemblies.Add(assemblyName);
                }

                var typeProvider = new MetadataSignatureProvider(reader);

                foreach (var typeHandle in reader.TypeDefinitions)
                {
                    var type = reader.GetTypeDefinition(typeHandle);
                    var namespaceName = reader.GetString(type.Namespace);
                    var typeName = reader.GetString(type.Name);
                    var fullTypeName = string.IsNullOrEmpty(namespaceName) ? typeName : namespaceName + "." + typeName;
                    result.TypesScanned++;

                    AddCandidate(result, new CandidateInput(
                        assemblyName,
                        namespaceName,
                        typeName,
                        "type",
                        typeName,
                        fullTypeName,
                        MatchTerms(fullTypeName),
                        likelyGameAssembly));

                    foreach (var methodHandle in type.GetMethods())
                    {
                        var method = reader.GetMethodDefinition(methodHandle);
                        var methodName = reader.GetString(method.Name);
                        var signature = DecodeMethodSignature(reader, method, typeProvider);
                        result.MembersScanned++;
                        AddCandidate(result, new CandidateInput(assemblyName, namespaceName, typeName, "method", methodName, signature, MatchTerms(fullTypeName, methodName, signature), likelyGameAssembly));
                    }

                    foreach (var propertyHandle in type.GetProperties())
                    {
                        var property = reader.GetPropertyDefinition(propertyHandle);
                        var propertyName = reader.GetString(property.Name);
                        var signature = DecodePropertySignature(property, typeProvider);
                        result.MembersScanned++;
                        AddCandidate(result, new CandidateInput(assemblyName, namespaceName, typeName, "property", propertyName, signature, MatchTerms(fullTypeName, propertyName, signature), likelyGameAssembly));
                    }

                    foreach (var fieldHandle in type.GetFields())
                    {
                        var field = reader.GetFieldDefinition(fieldHandle);
                        var fieldName = reader.GetString(field.Name);
                        var signature = DecodeFieldSignature(field, typeProvider);
                        result.MembersScanned++;
                        AddCandidate(result, new CandidateInput(assemblyName, namespaceName, typeName, "field", fieldName, signature, MatchTerms(fullTypeName, fieldName, signature), likelyGameAssembly));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to scan {dllPath}: {ex.Message}");
            }
        }

        if (result.LikelyGameAssemblies.Count == 0)
        {
            result.Warnings.Add("This looks like support/runtime interop, not generated Shadows of Doubt gameplay interop.");
            result.Warnings.Add("No gameplay patch points should be selected from this report.");
        }

        result.Candidates = result.Candidates
            .Where(candidate => options.IncludeLowConfidence || candidate.Confidence != "Low")
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Category, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Type, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.MemberName, StringComparer.Ordinal)
            .Take(options.MaxResults)
            .ToList();

        return result;
    }

    private static void AddCandidate(ScanResult result, CandidateInput input)
    {
        if (input.MatchedTerms.Count == 0)
        {
            return;
        }

        var score = Score(input);
        if (score <= 0)
        {
            return;
        }

        result.Candidates.Add(new Candidate
        {
            Assembly = input.Assembly,
            Namespace = input.Namespace,
            Type = input.Type,
            MemberKind = input.MemberKind,
            MemberName = input.MemberName,
            Signature = input.Signature,
            MatchedTerms = input.MatchedTerms,
            Score = score,
            Confidence = Confidence(score),
            PatchRisk = PatchRisk(input),
            SuggestedUse = SuggestedUse(input),
            Category = Category(input),
            Notes = Notes(input)
        });
    }

    private static int Score(CandidateInput input)
    {
        var typeMatches = CountMatches(input.Type, StrongTypeTerms);
        var memberMatches = CountMatches(input.MemberName, StrongMemberTerms);
        var signatureMatches = CountMatches(input.Signature, Terms);
        var score = input.MatchedTerms.Count;
        score += typeMatches * 8;
        score += memberMatches * 7;
        score += signatureMatches * 2;
        if (typeMatches > 0 && (memberMatches > 0 || signatureMatches > 0)) score += 10;
        if (input.LikelyGameAssembly) score += 5;
        if (IsLikelySystemAssembly(input.Assembly)) score -= 8;
        if (input.MemberKind == "method") score += 2;
        return score;
    }

    private static string Confidence(int score) => score >= 24 ? "High" : score >= 12 ? "Medium" : "Low";

    private static string PatchRisk(CandidateInput input)
    {
        if (CountMatches(input.Signature + " " + input.MemberName + " " + input.Type, DangerousTerms) >= 2)
        {
            return "High";
        }

        if (input.MemberKind is "property" or "field" || ReadOnlyTerms.Any(term => input.MemberName.StartsWith(term, StringComparison.OrdinalIgnoreCase)))
        {
            return "Low";
        }

        if (CountMatches(input.MemberName + " " + input.Type, new[] { "Interaction", "Interact", "Conversation", "Talk", "Question", "Answer", "Dialogue", "Dialog" }) > 0)
        {
            return "Medium";
        }

        return "Medium";
    }

    private static string SuggestedUse(CandidateInput input)
    {
        var text = input.Type + " " + input.MemberName + " " + input.Signature;
        if (CountMatches(text, new[] { "Question", "Answer", "Witness", "Info", "Evidence", "Seen", "Alibi" }) > 0)
        {
            return "possible for lie decision logging; dangerous for dialogue manipulation until manually reviewed";
        }

        if (CountMatches(text, new[] { "Interaction", "Interact", "Talk", "Conversation", "Dialogue", "Dialog" }) > 0)
        {
            return "possible for relationship/familiarity registration after manual hook review";
        }

        if (CountMatches(text, new[] { "Human", "Citizen", "Identity", "Name", "Address", "Workplace" }) > 0)
        {
            return "safe for read-only logging if hook target is manually confirmed";
        }

        return "low-confidence discovery candidate; manual review required";
    }

    private static string Category(CandidateInput input)
    {
        var text = input.Type + " " + input.MemberName + " " + input.Signature;
        if (CountMatches(text, new[] { "Witness", "Question", "Answer", "Info", "Seen", "Alibi" }) > 0) return "Witness/questions/information reveal";
        if (CountMatches(text, new[] { "Dialogue", "Dialog", "Conversation", "Speech", "Talk", "DDS", "DialogPreset" }) > 0) return "Dialogue/conversation";
        if (CountMatches(text, new[] { "Interaction", "Interactable", "Interact", "PlayerInteraction" }) > 0) return "Player-to-citizen interaction";
        if (CountMatches(text, new[] { "Evidence", "Case", "Murder", "Crime", "CasePanel" }) > 0) return "Evidence/case-related";
        if (CountMatches(text, new[] { "SideJob", "Job", "Message", "Telephone" }) > 0) return "Jobs/messages/emails";
        if (CountMatches(text, new[] { "NewAI", "Routine", "Location", "Company", "Residence", "PlayerApartment" }) > 0) return "AI/routines/locations";
        if (CountMatches(text, new[] { "Enforcer", "Authority" }) > 0) return "Enforcer/authority future compatibility";
        if (CountMatches(text, new[] { "Human", "Citizen", "Actor", "Identity", "Name", "Address", "Workplace", "Acquaintance", "Relationship" }) > 0) return "Citizen/Human identity";
        return "Low-confidence leftovers";
    }

    private static string Notes(CandidateInput input)
    {
        if (input.LikelyGameAssembly)
        {
            return "Assembly name does not look like a system/library assembly. Manual review still required.";
        }

        return "Name-based metadata match only. Do not patch without manual confirmation.";
    }

    private static IReadOnlyList<string> MatchTerms(params string[] values)
    {
        var text = string.Join(" ", values);
        return Terms
            .Where(term => text.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(term => term, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int CountMatches(string value, IEnumerable<string> terms) => terms.Count(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static bool IsLikelyGameAssembly(string assemblyName) => !IsLikelySystemAssembly(assemblyName);

    private static bool IsLikelySystemAssembly(string assemblyName)
    {
        if (assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefixes = new[] { "System", "Microsoft", "Unity", "UnityEngine", "Il2Cpp", "BepInEx", "Harmony", "Mono" };
        return prefixes.Any(prefix => assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetAssemblyName(MetadataReader reader, string dllPath)
    {
        if (reader.IsAssembly)
        {
            return reader.GetString(reader.GetAssemblyDefinition().Name);
        }

        return Path.GetFileNameWithoutExtension(dllPath);
    }

    private static string DecodeMethodSignature(MetadataReader reader, MethodDefinition method, MetadataSignatureProvider provider)
    {
        try
        {
            var decoded = method.DecodeSignature(provider, genericContext: null);
            var parameters = method.GetParameters()
                .Select(handle => reader.GetParameter(handle))
                .Where(parameter => parameter.SequenceNumber > 0)
                .OrderBy(parameter => parameter.SequenceNumber)
                .Select((parameter, index) =>
                {
                    var name = reader.GetString(parameter.Name);
                    var type = index < decoded.ParameterTypes.Length ? decoded.ParameterTypes[index] : "?";
                    return string.IsNullOrWhiteSpace(name) ? type : type + " " + name;
                });
            return decoded.ReturnType + " (" + string.Join(", ", parameters) + ")";
        }
        catch
        {
            return "(signature unavailable)";
        }
    }

    private static string DecodePropertySignature(PropertyDefinition property, MetadataSignatureProvider provider)
    {
        try
        {
            var decoded = property.DecodeSignature(provider, genericContext: null);
            return decoded.ReturnType + " (" + string.Join(", ", decoded.ParameterTypes) + ")";
        }
        catch
        {
            return "(signature unavailable)";
        }
    }

    private static string DecodeFieldSignature(FieldDefinition field, MetadataSignatureProvider provider)
    {
        try
        {
            return field.DecodeSignature(provider, genericContext: null);
        }
        catch
        {
            return "(signature unavailable)";
        }
    }
}

internal sealed record CandidateInput(
    string Assembly,
    string Namespace,
    string Type,
    string MemberKind,
    string MemberName,
    string Signature,
    IReadOnlyList<string> MatchedTerms,
    bool LikelyGameAssembly);

internal sealed class ScanResult
{
    public int AssembliesScanned { get; set; }
    public int TypesScanned { get; set; }
    public int MembersScanned { get; set; }
    public SortedSet<string> AssemblyNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    public SortedSet<string> LikelyGameAssemblies { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Candidate> Candidates { get; set; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
}

internal sealed class Candidate
{
    public string Assembly { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string MemberKind { get; init; } = string.Empty;
    public string MemberName { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchedTerms { get; init; } = Array.Empty<string>();
    public int Score { get; init; }
    public string Confidence { get; init; } = string.Empty;
    public string PatchRisk { get; init; } = string.Empty;
    public string SuggestedUse { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

internal static class ReportWriter
{
    private static readonly string[] Categories =
    {
        "Citizen/Human identity",
        "Player-to-citizen interaction",
        "Dialogue/conversation",
        "Witness/questions/information reveal",
        "Evidence/case-related",
        "Jobs/messages/emails",
        "AI/routines/locations",
        "Enforcer/authority future compatibility",
        "Low-confidence leftovers"
    };

    public static void Write(string outputPath, ScanOptions options, ScanResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
        File.WriteAllLines(outputPath, Build(options, result));
    }

    public static IEnumerable<string> BuildPlaceholder(ScanOptions options, string? reason)
    {
        yield return "# Interop Scan Report";
        yield return string.Empty;
        yield return $"Scan timestamp: `{DateTimeOffset.Now:O}`";
        yield return string.Empty;
        yield return "## Status";
        yield return string.Empty;
        yield return "No metadata scan was performed.";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            yield return string.Empty;
            yield return $"Reason: {reason}";
        }

        yield return string.Empty;
        yield return "## How To Run";
        yield return string.Empty;
        yield return "```powershell";
        yield return "dotnet run --project DevTools/AssemblyInspector/AssemblyInspector.csproj -- \"C:\\Path\\To\\BepInEx\\interop\"";
        yield return "```";
        yield return string.Empty;
        yield return "No Harmony patches were generated.";
    }

    private static IEnumerable<string> Build(ScanOptions options, ScanResult result)
    {
        yield return "# Interop Scan Report";
        yield return string.Empty;
        yield return $"Scan timestamp: `{DateTimeOffset.Now:O}`";
        yield return $"Input folder: `{options.InputFolder}`";
        yield return string.Empty;
        yield return "## Summary";
        yield return string.Empty;
        yield return $"- Assemblies scanned: `{result.AssembliesScanned}`";
        yield return $"- Types scanned: `{result.TypesScanned}`";
        yield return $"- Members scanned: `{result.MembersScanned}`";
        yield return $"- Candidates found: `{result.Candidates.Count}`";
        yield return $"- Include low confidence: `{options.IncludeLowConfidence}`";
        yield return string.Empty;
        yield return "This report is discovery-only. It does not create Harmony patches, mutate dialogue, or confirm that any candidate is safe to patch.";
        yield return string.Empty;

        yield return "## Likely Game Assemblies";
        yield return string.Empty;
        if (result.LikelyGameAssemblies.Count == 0)
        {
            yield return "None detected. This scan appears to contain only support/runtime assemblies.";
        }
        else
        {
            foreach (var assemblyName in result.LikelyGameAssemblies)
            {
                yield return $"- `{assemblyName}`";
            }
        }

        yield return string.Empty;

        yield return "## Warnings And Errors";
        yield return string.Empty;
        if (result.Warnings.Count == 0 && result.Errors.Count == 0)
        {
            yield return "None.";
        }
        else
        {
            foreach (var warning in result.Warnings)
            {
                yield return "- Warning: " + warning;
            }

            foreach (var error in result.Errors)
            {
                yield return "- Error: " + error;
            }
        }

        yield return string.Empty;
        yield return "## Top Candidate Summary";
        yield return string.Empty;
        foreach (var candidate in result.Candidates.Take(10))
        {
            yield return $"- `{candidate.Assembly}` `{candidate.Type}.{candidate.MemberName}` ({candidate.MemberKind}) score `{candidate.Score}`, confidence `{candidate.Confidence}`, risk `{candidate.PatchRisk}`";
        }

        if (result.Candidates.Count == 0)
        {
            yield return "No candidates matched the configured search terms.";
        }

        yield return string.Empty;

        foreach (var category in Categories)
        {
            var categoryCandidates = result.Candidates.Where(candidate => candidate.Category == category).ToArray();
            if (categoryCandidates.Length == 0)
            {
                continue;
            }

            yield return "## " + category;
            yield return string.Empty;
            foreach (var candidate in categoryCandidates)
            {
                yield return $"### `{candidate.Type}.{candidate.MemberName}`";
                yield return string.Empty;
                yield return $"- Assembly: `{candidate.Assembly}`";
                yield return $"- Namespace: `{candidate.Namespace}`";
                yield return $"- Type: `{candidate.Type}`";
                yield return $"- Member kind: `{candidate.MemberKind}`";
                yield return $"- Member name: `{candidate.MemberName}`";
                yield return $"- Signature: `{candidate.Signature}`";
                yield return $"- Matched terms: `{string.Join(", ", candidate.MatchedTerms)}`";
                yield return $"- Score: `{candidate.Score}`";
                yield return $"- Confidence: `{candidate.Confidence}`";
                yield return $"- Patch risk: `{candidate.PatchRisk}`";
                yield return $"- Suggested use: {candidate.SuggestedUse}";
                yield return $"- Notes: {candidate.Notes}";
                yield return string.Empty;
            }
        }
    }
}

internal sealed class MetadataSignatureProvider : ISignatureTypeProvider<string, object?>
{
    private readonly MetadataReader reader;

    public MetadataSignatureProvider(MetadataReader reader)
    {
        this.reader = reader;
    }

    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) => genericType + "<" + string.Join(", ", typeArguments) + ">";
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetPinnedType(string elementType) => elementType;
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeDefinition(handle);
        var namespaceName = reader.GetString(type.Namespace);
        var name = reader.GetString(type.Name);
        return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeReference(handle);
        var namespaceName = reader.GetString(type.Namespace);
        var name = reader.GetString(type.Name);
        return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
    }

    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
