using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using IntelOrca.Biohazard.BioRand.RE4R.Patches;
using IntelOrca.Biohazard.REE;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class ExportedMods
    {
        private static ImmutableArray<Type> _patches;
        private static ImmutableArray<ExportModAttribute> _all;

        private static ImmutableArray<Type> PatchTypes
        {
            get
            {
                if (_patches.IsDefault)
                {
                    var patches = ImmutableArray.CreateBuilder<Type>();
                    var assembly = Assembly.GetExecutingAssembly();
                    foreach (var t in assembly.GetTypes())
                    {
                        if (t.GetInterfaces().Any(x => x == typeof(IPatch)))
                        {
                            patches.Add(t);
                        }
                    }
                    _patches = patches.ToImmutable();
                }
                return _patches;
            }
        }

        public static ImmutableArray<ExportModAttribute> All
        {
            get
            {
                if (_all.IsDefault)
                {
                    _all = PatchTypes
                        .Select(x => x.GetCustomAttribute<ExportModAttribute>()!)
                        .Where(x => x != null)
                        .ToImmutableArray();
                }
                return _all;
            }
        }

        private static void ApplyPatch(Type type, ChainsawRandomizer? randomizer, IPatchContext context)
        {
            var ctors = type.GetConstructors();
            if (ctors.Length > 1)
                throw new NotSupportedException($"Only one constructor supported for {nameof(IPatch)}");

            var ctor = ctors[0];
            var ctorParameters = ctor.GetParameters().ToArray();
            var ctorArguments = new object?[ctorParameters.Length];
            for (var i = 0; i < ctorArguments.Length; i++)
            {
                if (ctorParameters[i].ParameterType == typeof(ChainsawRandomizer))
                {
                    ctorArguments[i] = randomizer;
                }
                if (ctorParameters[i].ParameterType == typeof(IPatchContext))
                {
                    ctorArguments[i] = context;
                }
            }

            var patchInstance = (IPatch)Activator.CreateInstance(type, ctorArguments)!;
            patchInstance.Apply();
        }

        public static ModBuilder ExportMod(string inputPath, string name)
        {
            var exportedModAttribute = All.FirstOrDefault(x => x.Name == name)
                ?? throw new ArgumentException($"{name} not found", nameof(name));
            var type = PatchTypes.FirstOrDefault(x => x.GetCustomAttribute<ExportModAttribute>()!.Name == exportedModAttribute.Name)
                ?? throw new Exception($"Type for {name} not found");

            var vanilla = new RePakCollection(inputPath);
            var modBuilder = new ModBuilder
            {
                Name = exportedModAttribute.Name,
                Description = exportedModAttribute.Description,
                Version = exportedModAttribute.Version,
                Author = exportedModAttribute.Author
            };

            var patchContext = new PatchContext(vanilla, modBuilder);
            ApplyPatch(type, null, patchContext);

            return modBuilder;
        }

        internal static void ApplyAll(ChainsawRandomizer? randomizer, IPatchContext context)
        {
            foreach (var patchType in PatchTypes)
            {
                ApplyPatch(patchType, randomizer, context);
            }
        }

        private class PatchContext(IPakFile vanilla, ModBuilder modBuilder) : IPatchContext
        {
            public RszTypeRepository TypeRepository => FileRepository.RszRepository;
            public DynamicData DynamicData { get; } = new DynamicData(download: false);

            public byte[]? GetSupplementFile(string path) => EmbeddedData.GetFile(path);
            public byte[]? GetFile(string path) => modBuilder[path] ?? vanilla.GetEntryData(path);
            public void SetFile(string path, byte[] data) => modBuilder[path] = data;
        }
    }
}
