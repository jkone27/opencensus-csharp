﻿// <copyright file="MetricsExporterThread.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Exporter.ApplicationInsights.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using OpenCensus.Stats;
    using OpenCensus.Stats.Aggregations;

    internal class MetricsExporterThread
    {
        private readonly IViewManager viewManager;

        private readonly TelemetryClient telemetryClient;

        private readonly TimeSpan collectionInterval = TimeSpan.FromSeconds(10);

        private readonly TimeSpan cancellationInterval = TimeSpan.FromMilliseconds(10);

        private readonly CancellationToken token;

        public MetricsExporterThread(TelemetryConfiguration telemetryConfiguration, IViewManager viewManager, CancellationToken token, TimeSpan collectionInterval)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.viewManager = viewManager;
            this.collectionInterval = collectionInterval;
            this.token = token;
        }

        public void WorkerThread()
        {
            try
            {
                var sleepInterval = this.collectionInterval;
                Stopwatch sw = new Stopwatch();

                while (!this.token.IsCancellationRequested)
                {
                    sw.Start();
                    this.Export();
                    sw.Stop();

                    // adjust interval for data collection time
                    sleepInterval = this.collectionInterval.Subtract(sw.Elapsed);

                    // allow faster thread cancellation
                    while (sleepInterval > this.cancellationInterval && !this.token.IsCancellationRequested)
                    {
                        Thread.Sleep(this.cancellationInterval);
                        sleepInterval.Subtract(this.cancellationInterval);
                    }

                    Thread.Sleep(sleepInterval);
                }
            }
            catch (Exception)
            {
                // TODO: report error
            }
        }

        internal void Export()
        {
            foreach (var view in this.viewManager.AllExportedViews)
            {
                var data = this.viewManager.GetView(view.Name);

                foreach (var value in data.AggregationMap)
                {
                    var metricTelemetry = new MetricTelemetry
                    {
                        Name = data.View.Name.AsString,
                    };

                    for (int i = 0; i < value.Key.Values.Count; i++)
                    {
                        var name = data.View.Columns[i].Name;
                        var val = value.Key.Values[i].AsString;
                        metricTelemetry.Properties.Add(name, val);
                    }

                    // Now those propertis needs to be populated.
                    //
                    // metricTelemetry.Sum
                    // metricTelemetry.Count
                    // metricTelemetry.Max
                    // metricTelemetry.Min
                    // metricTelemetry.StandardDeviation
                    //
                    // See data model for clarification on the meaning of those fields.
                    // https://docs.microsoft.com/azure/application-insights/application-insights-data-model-metric-telemetry

                    value.Value.Match<object>(
                        (combined) =>
                        {
                            if (combined is ISumDataDouble sum)
                            {
                                metricTelemetry.Sum = sum.Sum;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is ISumDataLong sum)
                            {
                                metricTelemetry.Sum = sum.Sum;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is ICountData count)
                            {
                                metricTelemetry.Sum = count.Count;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is IMeanData mean)
                            {
                                metricTelemetry.Sum = mean.Mean * mean.Count;
                                metricTelemetry.Count = (int)mean.Count;
                                metricTelemetry.Max = mean.Max;
                                metricTelemetry.Min = mean.Min;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is IDistributionData dist)
                            {
                                metricTelemetry.Sum = dist.Mean * dist.Count;
                                metricTelemetry.Count = (int)dist.Count;
                                metricTelemetry.Min = dist.Min;
                                metricTelemetry.Max = dist.Max;
                                metricTelemetry.StandardDeviation = dist.SumOfSquaredDeviations;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is ILastValueDataDouble lastValue)
                            {
                                metricTelemetry.Sum = lastValue.LastValue;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is ILastValueDataLong lastValue)
                            {
                                metricTelemetry.Sum = lastValue.LastValue;
                            }

                            return null;
                        },
                        (combined) =>
                        {
                            if (combined is IAggregationData aggregationData)
                            {
                                // TODO: report an error
                            }

                            return null;
                        });
                    this.telemetryClient.TrackMetric(metricTelemetry);
                }

                Console.WriteLine(view);
                Console.WriteLine(data);
            }
        }
    }
}
