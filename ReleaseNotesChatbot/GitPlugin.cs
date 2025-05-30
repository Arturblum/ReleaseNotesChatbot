using System.ComponentModel;
using LibGit2Sharp;
using Microsoft.SemanticKernel;

namespace ReleaseNotesChatbot;

public class GitPlugin
{
    private string _repoPath;
    
    //kernel funtion to set the repo path
    [KernelFunction("set_repo_path")]
    [Description(
        "Sets the path to the Git repository when the first request to get commits is passed or if the user asks to work with a different repo. if the rpeo isnt set the path will not be changed")]
    public string SetRepoPath(
        [Description("The path to the Git repository.")]
        string repoPath)
    {
        try
        {
            var repoRoot = Repository.Discover(repoPath);
            if (repoRoot == null)
            {
                Console.WriteLine($"No Git repo found at or above: {repoPath}");
                return $"No Git repo found at or above: {repoPath}";
            }

            _repoPath = repoPath;
            Console.WriteLine($"Repository path set to: {_repoPath}");
            return $"Repository path set to: {_repoPath}";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    [KernelFunction("get_latest_commits")]
    [Description("Gets a list of the latests commits from a Git repository.")]
    public List<string> GetLatestCommits(
        // [Description("The path to the Git repository.")]
        // string repoPath,
        [Description("The number of latest commits to retrieve.")]
        int count)
    {
        var stringifiedCommits = new List<string>();

        try
        {
            var repoRoot = Repository.Discover(_repoPath);
            if (repoRoot == null)
            {
                Console.WriteLine($"No Git repo found at or above: {_repoPath}");
                return stringifiedCommits;
            }

            using var repo = new Repository(repoRoot);
            foreach (var commit in repo.Commits.Take(count))
            {
                stringifiedCommits.Add(commit.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading commits: {ex.Message}");
        }

        return stringifiedCommits;
    }
}