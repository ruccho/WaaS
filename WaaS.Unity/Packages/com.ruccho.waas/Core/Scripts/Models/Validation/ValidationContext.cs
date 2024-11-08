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
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Description.TypeIndex;
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
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Description.GlobalType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.GlobalSection.Globals.Span[(int)(index - i)].Type;
            }
        }

        public TableType GetTableType(uint index)
        {
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Description.TableType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.TableSection.TableTypes.Span[(int)(index - i)];
            }
        }

        public MemoryType GetMemoryType(uint index)
        {
            checked
            {
                var importSection = Module.ImportSection;
                var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

                var i = 0;
                foreach (var import in imports)
                {
                    var t = import.Description.MemoryType;
                    if (!t.HasValue) continue;

                    if (i == index) return t.Value;

                    i++;
                }

                return Module.MemorySection.MemoryTypes.Span[(int)(index - i)];
            }
        }
    }
}