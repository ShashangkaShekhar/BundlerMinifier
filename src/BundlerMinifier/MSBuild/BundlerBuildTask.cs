﻿using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BundlerMinifier
{
    /// <summary>
    /// An MSBuild task for running web compilers on a given config file.
    /// </summary>
    public class BundlerBuildTask : Task
    {
        private bool _isSuccessful = true;

        /// <summary>
        /// The file path of the compilerconfig.json file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Execute the Task
        /// </summary>
        public override bool Execute()
        {
            FileInfo configFile = new FileInfo(FileName);

            Log.LogMessage(MessageImportance.High, "Bundler: Begin processing " + configFile.Name);

            if (!configFile.Exists)
            {
                Log.LogWarning(configFile.FullName + " does not exist");
                return true;
            }

            BundleFileProcessor processor = new BundleFileProcessor();
            processor.AfterProcess += Processor_AfterProcess;
            processor.AfterWritingSourceMap += Processor_AfterWritingSourceMap;
            BundleMinifier.ErrorMinifyingFile += BundleMinifier_ErrorMinifyingFile;
            BundleMinifier.AfterWritingMinFile += FileMinifier_AfterWritingMinFile;

            processor.Process(configFile.FullName);

            Log.LogMessage(MessageImportance.High, "Bundler: Done processing " + configFile.Name);

            return _isSuccessful;
        }

        private void BundleMinifier_ErrorMinifyingFile(object sender, MinifyFileEventArgs e)
        {
            if (e.Result == null || !e.Result.HasErrors)
                return;

            _isSuccessful = false;

            foreach (var error in e.Result.Errors)
            {
                Log.LogError("Bundler & Minifier", "0", "", error.FileName, error.LineNumber, error.ColumnNumber, error.LineNumber, error.ColumnNumber, error.Message, null); ;
            }
        }

        private void Processor_AfterProcess(object sender, BundleFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "\tBundled " + e.Bundle.OutputFileName);
        }

        private void Processor_AfterWritingSourceMap(object sender, MinifyFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "\tSourceMap " + MakeRelative(FileName, e.ResultFile));
        }

        private void FileMinifier_AfterWritingMinFile(object sender, MinifyFileEventArgs e)
        {
            Log.LogMessage(MessageImportance.High, "\tMinified " + MakeRelative(FileName, e.ResultFile));
        }

        private static string MakeRelative(string baseFile, string file)
        {
            Uri baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            Uri fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            return baseUri.MakeRelativeUri(fileUri).ToString();
        }
    }
}
