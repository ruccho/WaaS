﻿using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Type section of a WebAssembly module.
    /// </summary>
    public class TypeSection : Section
    {
        internal TypeSection(ref ModuleReader reader)
        {
            var numFuncTypes = reader.ReadVectorSize();
            var funcTypes = new FunctionType[numFuncTypes];

            for (var i = 0; i < numFuncTypes; i++) funcTypes[i] = new FunctionType(ref reader);
            FuncTypes = funcTypes;
        }

        public ReadOnlyMemory<FunctionType> FuncTypes { get; }
    }
}