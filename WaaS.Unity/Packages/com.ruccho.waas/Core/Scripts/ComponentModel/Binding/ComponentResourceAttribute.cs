﻿using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentResourceAttribute : Attribute
    {
        public ComponentResourceAttribute()
        {
        }

        public ComponentResourceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}