using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace FileLogs.Otel.Collector.Helpers;

public static class ConfigurationHelper
{
    private const string LogConfigurationsSectionName = "logConfigurations";
    
    private sealed class DirectoryConfig
    {
        [JsonPropertyName("path")]
        public required string Path { get; init; }

        [JsonPropertyName("recursive")]
        public bool IsRecursive { get; init; }
        
        [JsonPropertyName("searchPattern")]
        public string? SearchPattern { get; init; }
    }
    
    public static IEnumerable<LogConfiguration> GetLogConfigurations(this IConfiguration configuration)
    {
        var section = configuration.GetSection(LogConfigurationsSectionName);
        
        if (section == null)
        {
            throw new InvalidOperationException($"Section '{LogConfigurationsSectionName}' not found in configuration.");
        }
        
        var logConfigurationSections = section.GetChildren().ToImmutableList();

        foreach (var logConfigSection in logConfigurationSections)
        {
            yield return ConstructLogConfiguration(logConfigSection);
        }
    }

    private static LogConfiguration ConstructLogConfiguration(IConfigurationSection section)
    {
        var logLineStartRegexValue =
            section.GetValue<string>(GetLogConfigJsonPropertyName(config => config.LogLineStartRegex));

        var logLineStartRegex = !string.IsNullOrWhiteSpace(logLineStartRegexValue) 
            ? new Regex(logLineStartRegexValue) 
            : throw new InvalidOperationException("Required to specify log line start regex");

        var logLineTimestampRegexValue =
            section.GetValue<string>(GetLogConfigJsonPropertyName(config => config.LogLineTimestampRegex));
        
        var logLineTimestampRegex = string.IsNullOrWhiteSpace(logLineTimestampRegexValue) 
            ? null 
            : new Regex(logLineTimestampRegexValue);
        
        var logLineTypeRegexValue =
            section.GetValue<string>(GetLogConfigJsonPropertyName(config => config.LogLineTypeRegex));

        var logLineTypeRegex = string.IsNullOrWhiteSpace(logLineTypeRegexValue) 
            ? null 
            : new Regex(logLineTypeRegexValue);

        return new LogConfiguration
        {
            Files = GetAndValidateFiles(section),
            LogLineStartRegex = logLineStartRegex,
            LogLineStartTypeMatchingGroup = section.GetValue<string?>(
                GetLogConfigJsonPropertyName(config => config.LogLineStartTypeMatchingGroup)),
            
            LogLineStartTimestampMatchingGroup = section.GetValue<string?>(
                GetLogConfigJsonPropertyName(config => config.LogLineStartTimestampMatchingGroup)),
            
            LogLineTimestampRegex = logLineTimestampRegex,
            LogLineTimestampMatchingGroup = section.GetValue<string?>(
                GetLogConfigJsonPropertyName(config => config.LogLineTimestampMatchingGroup)),
            
            LogLineTypeRegex = logLineTypeRegex,
            LogLineTypeMatchingGroup = section.GetValue<string?>(
                GetLogConfigJsonPropertyName(config => config.LogLineTypeMatchingGroup))
        };
    }
    
    private static List<FileInfo> GetAndValidateFiles(IConfigurationSection section)
    {
        var fileConfigSections = section
            .GetSection(GetLogConfigJsonPropertyName(config => config.Files))
            .GetChildren()
            .ToImmutableList();
        
        var files = new List<FileInfo>(fileConfigSections.Count);
        var retrievedFilePaths = new HashSet<string>(fileConfigSections.Count);

        foreach (var filePathValue in fileConfigSections.Select(fileConfigSection => fileConfigSection.Value))
        {
            if (string.IsNullOrWhiteSpace(filePathValue))
            {
                throw new InvalidOperationException("Configured log file value is null or empty");
            }
            
            var file = new FileInfo(filePathValue);
            
            if (!file.Exists)
            {
                throw new FileNotFoundException($"File '{file.FullName}' does not exist.");
            }

            if (!retrievedFilePaths.Add(file.FullName))
            {
                throw new InvalidOperationException($"Duplicate file configured '{file.FullName}'");
            }
            
            files.Add(file);
        }
        
        var directoryConfigSections = section
            .GetSection(LogConfiguration.DirectoriesSectionName)
            .GetChildren()
            .ToImmutableList();

        AddFilesFromConfiguredDirectories(directoryConfigSections, files, retrievedFilePaths);

        if (files.Count == 0)
        {
            throw new InvalidOperationException($"No files found in configuration under '{GetLogConfigJsonPropertyName(config => config.Files)}'.");
        }

        return files;
    }

    private static void AddFilesFromConfiguredDirectories(
        IReadOnlyList<IConfigurationSection> directoryConfigSections,
        List<FileInfo> retrievedFiles,
        IReadOnlySet<string> retrievedFilePaths)
    {
        foreach (var directoryConfigSection in directoryConfigSections.Select(section => section))
        {
            var path = directoryConfigSection.GetValue<string?>(GetDirectoryConfigJsonPropertyName(config => config.Path));
            var isRecursive = directoryConfigSection.GetValue<bool?>(GetDirectoryConfigJsonPropertyName(config => config.IsRecursive));
            var searchPattern = directoryConfigSection.GetValue<string?>(GetDirectoryConfigJsonPropertyName(config => config.SearchPattern));
            
            var config = new DirectoryConfig
            {
                Path = !string.IsNullOrWhiteSpace(path) 
                    ? path 
                    : throw new InvalidOperationException("Path has to be configured on a directory configuration"),
                    
                IsRecursive = isRecursive ?? false,
                SearchPattern = string.IsNullOrWhiteSpace(searchPattern) ? null : searchPattern,
            };
            
            AddFilesFromDirectory(retrievedFiles, retrievedFilePaths, config);
        }
    }
    
    private static void AddFilesFromDirectory(
        List<FileInfo> retrievedFiles,
        IReadOnlySet<string> retrievedFilePaths,
        DirectoryConfig directoryConfig)
    {
        var directoryInfo = new DirectoryInfo(directoryConfig.Path);

        if (!directoryInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Directory '{directoryInfo.FullName}' does not exist.");
        }
        
        const string defaultSearchPattern = "*";
        
        var searchOption = directoryConfig.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = directoryInfo.GetFiles(directoryConfig.SearchPattern ?? defaultSearchPattern, searchOption);

        foreach (var file in files)
        {
            if (!retrievedFilePaths.Contains(file.FullName))
            {
                retrievedFiles.Add(file);
            }
        }
    }
    
    private static string GetDirectoryConfigJsonPropertyName(Expression<Func<DirectoryConfig, object?>> propertyExpression)
    {
        return GetJsonPropertyName(propertyExpression);
    }
    
    private static string GetLogConfigJsonPropertyName(Expression<Func<LogConfiguration, object?>> propertyExpression)
    {
        return GetJsonPropertyName(propertyExpression);
    }
    
    private static string GetJsonPropertyName<T>(Expression<Func<T, object?>> propertyExpression)
    {
        var member = propertyExpression.Body as MemberExpression ?? (propertyExpression.Body as UnaryExpression)?.Operand as MemberExpression;
        var property = member?.Member as PropertyInfo;
        var attribute = property?.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false).FirstOrDefault() as JsonPropertyNameAttribute;
        
        return attribute?.Name ?? property?.Name ?? throw new InvalidOperationException("Non matching property expression");
    }
}