using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Aop.Rewriter.ChangeTracking;
using Aop.Rewriter.MonoCecil;
using Mono.Cecil;

namespace Aop.Rewriter
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Out.WriteLine("Usage: Aop.Rewriter.exe -assemblyPath");
				return -1;
			}
			var stopwatch = Stopwatch.StartNew();
			var assemblyPath = args[0];
			if (assemblyPath.StartsWith("-"))
				assemblyPath = assemblyPath.Substring(1);
			var assemblyDefinition = MonoCecilHelpers.LoadAssembly(assemblyPath);
			var rewriter = new ChangeTrackingRewriter(new ChangeTrackingParameters
				{
					Assembly = assemblyDefinition,
					TypesToProcess = assemblyDefinition.MainModule.GetTypes().ToArray(),
					ImmutableTypeNames = new HashSet<string>
						{
							"BsonTimestamp"
						},
					IsForeignDelegate = r => !r.FullName.StartsWith("")
				});
			try
			{
				rewriter.Rewrite();
			}
			catch (ChangeTrackingRewriterException e)
			{
				Console.Out.WriteLine(e);
				return -1;
			}
			using (var stream = new FileStream(assemblyPath, FileMode.Create, FileAccess.Write))
				assemblyDefinition.Write(stream, new WriterParameters {WriteSymbols = true});
			Console.Out.WriteLine("done, {0} millis", stopwatch.ElapsedMilliseconds);
			return 0;
		}
	}
}