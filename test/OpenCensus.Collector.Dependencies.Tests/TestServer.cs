﻿// <copyright file="TestServer.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Collector.Dependencies.Tests
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class TestServer
    {
        private static Random GlobalRandom = new Random();

        private class RunningServer : IDisposable
        {
            private readonly Task httpListenerTask;
            private readonly HttpListener listener;
            private readonly CancellationTokenSource cts;
            private readonly AutoResetEvent initialized = new AutoResetEvent(false);

            public RunningServer(Action<HttpListenerContext> action, string host, int port)
            {
                this.cts = new CancellationTokenSource();
                this.listener = new HttpListener();

                CancellationToken token = this.cts.Token;

                this.listener.Prefixes.Add($"http://{host}:{port}/");
                this.listener.Start();

                this.httpListenerTask = new Task(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        var ctxTask = this.listener.GetContextAsync();

                        this.initialized.Set();

                        try
                        {
                            ctxTask.Wait(token);

                            if (ctxTask.Status == TaskStatus.RanToCompletion)
                            {
                                action(ctxTask.Result);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            Assert.True(false, ex.ToString());
                        }
                    }
                });
            }

            public void Start()
            {
                this.httpListenerTask.Start();
                this.initialized.WaitOne();
            }

            public void Dispose()
            {
                try
                {
                    this.listener?.Stop();
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // swallow this exception just in case
                }
            }
        }

        public static IDisposable RunServer(Action<HttpListenerContext> action, out string host, out int port)
        {
            host = "localhost";
            port = GlobalRandom.Next(2000, 5000);

            var server = new RunningServer(action, host, port);
            server.Start();

            return server;
        }
    }
}
