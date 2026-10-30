using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Context object for instruction validation.
    /// </summary>
    public readonly struct ValidationContext
    {
        public Module Module { get; }
        public Function CurrentFunction { get; }

        public ValidationContext(Module module, Function currentFunction)
        {
            Module = module;
            CurrentFunction = currentFunction;
        }

        public FunctionType GetFunctionType(uint runtimeFunctionIndex)
        {
            if (runtimeFunctionIndex >= Module.FunctionCount)
                throw new InvalidModuleException($"unknown function {runtimeFunctionIndex}");
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Descriptor.TypeIndex;
                    if (!t.HasValue) continue;

                    if (i == runtimeFunctionIndex) return Module.TypeSection.FuncTypes.Span[(int)t.Value];

                    i++;
                }

                return Module.InternalFunctions.Span[(int)(runtimeFunctionIndex - i)].Type;
            }
        }

        public ValueType GetLocalType(uint index)
        {
            checked
            {
                // parameter
                var paramTypes = CurrentFunction.Type.ParameterTypes.Span;
                if (index < paramTypes.Length) return paramTypes[(int)index];
                index -= (uint)paramTypes.Length;

                foreach (var local in CurrentFunction.Body.Locals.Span)
                {
                    if (index < local.Count) return local.Type;
                    index -= local.Count;
                }

                throw new InvalidModuleException();
            }
        }


        public GlobalType GetGlobalType(uint index)
        {
            if (index >= Module.GlobalCount) throw new InvalidModuleException($"unknown global {index}");
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Descriptor.GlobalType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.GlobalSection.Globals.Span[(int)(index - i)].Type;
            }
        }

        public TableType GetTableType(uint index)
        {
            if (index >= Module.TableCount) throw new InvalidModuleException($"unknown table {index}");
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Descriptor.TableType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.TableSection.TableTypes.Span[(int)(index - i)];
            }
        }

        public MemoryType GetMemoryType(uint index)
        {
            if (index >= Module.MemoryCount) throw new InvalidModuleException($"unknown memory {index}");
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Descriptor.MemoryType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.MemorySection.MemoryTypes.Span[(int)(index - i)];
            }
        }
        /// <summary>
        ///     Validates <c>ref.func</c> and returns its type.
        /// </summary>
        /// <param name="inFunctionBody">
        ///     The functions referenced in function bodies must be declared outside the function bodies (in elements,
        ///     exports or the initializers of globals).
        /// </param>
        public ValueType ValidateFunctionReference(uint functionIndex, bool inFunctionBody)
        {
            if (functionIndex >= Module.FunctionCount) throw new InvalidModuleException($"unknown function {functionIndex}");
            if (inFunctionBody && !Module.IsFunctionReferenceDeclared(functionIndex))
                throw new InvalidModuleException("undeclared function reference");
            return ValueType.FuncRef;
        }

        public Element GetElement(uint index)
        {
            var elements = Module.ElementSection != null ? Module.ElementSection.Elements.Span : ReadOnlySpan<Element>.Empty;
            if (index >= elements.Length) throw new InvalidModuleException($"unknown elem segment {index}");
            return elements[(int)index];
        }

        public void ValidateDataIndex(uint index)
        {
            // the data count section is required to refer to the data segments in the code section
            if (Module.DataCountSection == null) throw new InvalidModuleException("data count section required");
            if (index >= Module.DataCountSection.Count) throw new InvalidModuleException($"unknown data segment {index}");
        }
    }
}
