using LibGit2Sharp;

namespace ReleaseNotesChatbot.DataIngestion;

public class GitHelper
{
    public static Dictionary<string, string> GetAllCodeFromLatestCommit(string repoPath)
    {
        var files = new Dictionary<string, string>();

        try
        {
            var repoRoot = Repository.Discover(repoPath);
            if (repoRoot == null)
            {
                Console.WriteLine("Git repository not found.");
                return files;
            }

            using var repo = new Repository(repoRoot);
            var commit = repo.Head.Tip;
            var tree = commit.Tree;

            // Recursively read all files from the tree
            foreach (var entry in tree)
            {
                //filtering out dlls
                if (entry.TargetType == TreeEntryTargetType.Tree && entry.Name is "bin" or "obj") continue;

                    ReadTreeEntryRecursive(entry, files);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return files;
    }

    private static void ReadTreeEntryRecursive(TreeEntry entry, Dictionary<string, string> files, string path = "")
    {
        if (entry.TargetType == TreeEntryTargetType.Blob)
        {
            var blob = (Blob)entry.Target;
            var contentStream = blob.GetContentStream();
            using var reader = new StreamReader(contentStream);
            var content = reader.ReadToEnd();

            var fullPath = Path.Combine(path, entry.Name);
            files[fullPath] = content;
        }
        else if (entry.TargetType == TreeEntryTargetType.Tree)
        {
            var tree = (Tree)entry.Target;
            foreach (var subEntry in tree)
            {
                ReadTreeEntryRecursive(subEntry, files, Path.Combine(path, entry.Name));
            }
        }
    }
}
