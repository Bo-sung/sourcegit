using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGit.Commands
{
    public class Config : Command
    {
        public Config(string repository)
        {
            if (string.IsNullOrEmpty(repository))
            {
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else
            {
                WorkingDirectory = repository;
                Context = repository;
                _isLocal = true;
            }
        }

        public Dictionary<string, string> ReadAll()
        {
            Args = "config -l";
            return Parse(ReadToEnd());
        }

        /// <summary>
        /// The repository's own config file only (`config --local -l`). A read is normally the
        /// system, the global and the local file put together, so a snapshot taken that way cannot be
        /// used to decide what an update of this repository overwrites: writing goes to the local
        /// file alone, and a key that only the global file holds cannot be unset or rolled back
        /// here. Only the write path of a setting uses this; showing the keys keeps the merged read,
        /// so nothing stays hidden from the user.
        /// </summary>
        public Dictionary<string, string> ReadLocalAll()
        {
            Args = "config --local -l";
            return Parse(ReadToEnd());
        }

        public async Task<Dictionary<string, string>> ReadAllAsync()
        {
            Args = "config -l";
            return Parse(await ReadToEndAsync().ConfigureAwait(false));
        }

        /// <summary>The local-file counterpart of <see cref="ReadAllAsync"/>, see <see cref="ReadLocalAll"/>.</summary>
        public async Task<Dictionary<string, string>> ReadLocalAllAsync()
        {
            Args = "config --local -l";
            return Parse(await ReadToEndAsync().ConfigureAwait(false));
        }

        private static Dictionary<string, string> Parse(Result output)
        {
            var rs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (output.IsSuccess)
            {
                var lines = output.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0];
                        var value = parts[1];
                        rs[key] = value;
                    }
                }
            }

            return rs;
        }

        public string Get(string key)
        {
            Args = $"config {key}";
            return ReadToEnd().StdOut.Trim();
        }

        public async Task<string> GetAsync(string key)
        {
            Args = $"config {key}";

            var rs = await ReadToEndAsync().ConfigureAwait(false);
            return rs.StdOut.Trim();
        }

        public async Task<bool> SetAsync(string key, string value, bool allowEmpty = false)
        {
            var scope = _isLocal ? "--local" : "--global";

            if (!allowEmpty && string.IsNullOrWhiteSpace(value))
                Args = $"config {scope} --unset {key}";
            else
                Args = $"config {scope} {key} {value.Quoted()}";

            return await ExecAsync().ConfigureAwait(false);
        }

        private bool _isLocal = false;
    }
}
