using System.CommandLine;
using System.Text.Json;
using ContentFigures.Infrastructure;
using ContentFigures.Services;
using ContentWriter.Application.DTOs;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var projectIdOption = new Option<Guid>("--project-id")
{
    IsRequired = true,
    Description = "Content-Writer project GUID",
};

var sourceOption = new Option<string>("--source")
{
    IsRequired = true,
    Description = "Figure source: pillar or blog",
};

var headingSlugOption = new Option<string>("--heading-slug")
{
    IsRequired = true,
    Description = "Stable heading slug from content_figures",
};

var fileOption = new Option<FileInfo>("--file")
{
    IsRequired = true,
    Description = "Local .webp image file to upload",
};

var altOption = new Option<string?>("--alt", "Override image alt text");

var outOption = new Option<FileInfo?>("--out", "Write manifest JSON to this file instead of stdout");

var dirOption = new Option<DirectoryInfo>("--dir")
{
    IsRequired = true,
    Description = "Directory containing h2-*.webp files",
};

var root = new RootCommand("ContentFigures — attach section art to Content-Writer figures (Phase 2)");

var listCmd = new Command("list", "List figures for a project");
listCmd.AddOption(projectIdOption);
listCmd.SetHandler(async projectId =>
{
    await using var db = ContentFiguresDb.CreateContext();
    var repo = new ContentFigureRepository(db);
    var rows = await repo.ListByProjectAsync(projectId);

    if (rows.Count == 0)
    {
        Console.WriteLine($"No figures for project {projectId}.");
        return;
    }

    Console.WriteLine(
        $"{"Source",-8} {"Order",5} {"Slug",-28} {"Status",-10} {"GeekApiSlug",-12} Heading");
    Console.WriteLine(new string('-', 110));

    foreach (var row in rows)
    {
        var slugState = string.IsNullOrWhiteSpace(row.GeekApiSlug) ? "MISSING" : "ok";
        Console.WriteLine(
            $"{row.SourceType,-8} {row.SectionOrder,5} {row.HeadingSlug,-28} {row.Status,-10} {slugState,-12} {row.Heading}");
    }

    var missingSlug = rows.Count(r => string.IsNullOrWhiteSpace(r.GeekApiSlug));
    if (missingSlug > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"{missingSlug} figure(s) missing GeekApiSlug — publish text before attach.");
    }
}, projectIdOption);

var attachCmd = new Command("attach", "Upload a .webp to Vercel Blob and mark the figure Ready");
attachCmd.AddOption(projectIdOption);
attachCmd.AddOption(sourceOption);
attachCmd.AddOption(headingSlugOption);
attachCmd.AddOption(fileOption);
attachCmd.AddOption(altOption);
attachCmd.SetHandler(async (projectId, source, headingSlug, file, alt) =>
{
    await ContentFigureAttachRunner.AttachFileAsync(
        projectId,
        source,
        headingSlug,
        file.FullName,
        alt);

    Console.WriteLine($"Attached {file.Name} -> {source}/{headingSlug}");
}, projectIdOption, sourceOption, headingSlugOption, fileOption, altOption);

var skipCmd = new Command("skip", "Mark a section as intentionally without art");
skipCmd.AddOption(projectIdOption);
skipCmd.AddOption(sourceOption);
skipCmd.AddOption(headingSlugOption);
skipCmd.SetHandler(async (projectId, source, headingSlug) =>
{
    await FigureSkipService.SkipAsync(projectId, source, headingSlug);
    Console.WriteLine($"Skipped {source}/{headingSlug}");
}, projectIdOption, sourceOption, headingSlugOption);

var exportCmd = new Command("export-manifest", "Export figure manifest JSON for Figma workflow");
exportCmd.AddOption(projectIdOption);
exportCmd.AddOption(outOption);
exportCmd.SetHandler(async (projectId, outFile) =>
{
    await using var db = ContentFiguresDb.CreateContext();
    var repo = new ContentFigureRepository(db);
    var rows = await repo.ListByProjectAsync(projectId);

    var manifest = new ContentFigureManifestResponse(
        projectId,
        rows.Select(f => new ContentFigureManifestEntry(
            f.SourceType,
            f.HeadingSlug,
            f.Heading,
            f.SectionOrder,
            f.BriefText,
            f.Status,
            f.ImageUrl,
            f.GeekApiSlug,
            f.NeedsFigureMerge)).ToList());

    var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    if (outFile is not null)
    {
        await File.WriteAllTextAsync(outFile.FullName, json);
        Console.WriteLine($"Wrote {outFile.FullName} ({manifest.Figures.Count} figures)");
    }
    else
    {
        Console.WriteLine(json);
    }
}, projectIdOption, outOption);

var syncDirCmd = new Command("sync-dir", "Attach every h2-*.webp in a directory to matching figures");
syncDirCmd.AddOption(projectIdOption);
syncDirCmd.AddOption(sourceOption);
syncDirCmd.AddOption(dirOption);
syncDirCmd.SetHandler(async (projectId, source, dir) =>
{
    await using var db = ContentFiguresDb.CreateContext();
    var attach = new FigureAttachService(db);
    await attach.SyncDirectoryAsync(projectId, source, dir.FullName);
    Console.WriteLine($"Synced directory {dir.FullName}");
}, projectIdOption, sourceOption, dirOption);

var headingSlugOptional = new Option<string?>("--heading-slug")
{
    Description = "Stable heading slug; omit to generate all Pending figures for the source",
};

var generateCmd = new Command("generate", "Generate section art from figure briefs via OpenAI");
generateCmd.AddOption(projectIdOption);
generateCmd.AddOption(sourceOption);
generateCmd.AddOption(headingSlugOptional);
generateCmd.SetHandler(async (projectId, source, headingSlug) =>
{
    var count = await ContentFigureImageGenerationRunner.GenerateAsync(projectId, source, headingSlug);
    Console.WriteLine($"Generated {count} figure(s) for {source}.");
}, projectIdOption, sourceOption, headingSlugOptional);

root.AddCommand(listCmd);
root.AddCommand(attachCmd);
root.AddCommand(skipCmd);
root.AddCommand(exportCmd);
root.AddCommand(syncDirCmd);
root.AddCommand(generateCmd);

var mergeCmd = new Command("merge", "Merge Ready figures into the live GeekAPI post body");
mergeCmd.AddOption(projectIdOption);
mergeCmd.AddOption(sourceOption);
mergeCmd.SetHandler(async (projectId, source) =>
{
    var result = await FigureMergeRunner.MergeAsync(projectId, source);
    Console.WriteLine(
        $"Merged {result.FiguresMerged} figure(s) into post {result.GeekPostId} ({result.GeekApiSlug})");
    Console.WriteLine($"  path: {result.PublicPath}");
}, projectIdOption, sourceOption);

root.AddCommand(mergeCmd);

try
{
    return await root.InvokeAsync(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
