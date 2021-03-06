﻿// <copyright file="NoopRunningSpanStore.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of theLicense at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace.Export
{
    using System;
    using System.Collections.Generic;

    internal sealed class NoopRunningSpanStore : RunningSpanStoreBase
    {
        private static readonly IRunningSpanStoreSummary EmptySummary = RunningSpanStoreSummary.Create(new Dictionary<string, IRunningPerSpanNameSummary>());

        public override IRunningSpanStoreSummary Summary
        {
            get
            {
                return EmptySummary;
            }
        }

        public override IList<ISpanData> GetRunningSpans(IRunningSpanStoreFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return new List<ISpanData>();
        }

        public override void OnEnd(ISpan span)
        {
        }

        public override void OnStart(ISpan span)
        {
        }
    }
}
