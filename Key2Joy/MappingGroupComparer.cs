﻿using BrightIdeasSoftware;
using System.Collections.Generic;

namespace Key2Joy
{
    internal class MappingGroupComparer : IComparer<OLVGroup>
    {
        public int Compare(OLVGroup x, OLVGroup y)
        {
            return x.GroupId.CompareTo(y.GroupId);
        }
    }
}