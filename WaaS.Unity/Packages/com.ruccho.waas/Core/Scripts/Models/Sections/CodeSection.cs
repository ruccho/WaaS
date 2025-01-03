﻿using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Code section in a WebAssembly module.
    /// </summary>
    public class CodeSection : Section
    {
        internal CodeSection(ref ModuleReader reader)
        {
            var numCodes = reader.ReadVectorSize();
            var functions = new FunctionBody[numCodes];

            for (var i = 0; i < numCodes; i++) functions[i] = new FunctionBody(ref reader);

            Functions = functions;
        }

        public ReadOnlyMemory<FunctionBody> Functions { get; }
    }
}