#tool NUnit.ConsoleRunner

var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");
var binDir = Directory($"DevExpressMods/bin/{configuration}/net40");
var pubDir = Directory("pub");

Task("Clean")
    .Does(() => CleanDirectory(binDir));

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => MSBuild(".", new MSBuildSettings().SetConfiguration(configuration).WithTarget("Restore")));

Task("Build")
    .IsDependentOn("Restore")
    .Does(() => MSBuild(".", new MSBuildSettings().SetConfiguration(configuration).WithTarget("Build")));

Task("Test")
    .IsDependentOn("Build")
    .Does(() => NUnit3(GetFiles($"**/bin/{configuration}/**/*tests*.dll"), new NUnit3Settings { NoResults = true }));

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => MSBuild("DevExpressMods", new MSBuildSettings()
        .SetConfiguration(configuration)
        .WithTarget("Pack")
        .WithProperty("PackageOutputPath", System.IO.Path.GetFullPath(pubDir))));

RunTarget(target);
