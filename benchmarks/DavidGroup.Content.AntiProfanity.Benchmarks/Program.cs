using System.Reflection;

using BenchmarkDotNet.Running;

#if DEBUG
using BenchmarkDotNet.Configs;
#endif

#if !DEBUG
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
#else
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args, new DebugInProcessConfig());
#endif
