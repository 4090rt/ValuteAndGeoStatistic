using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class BDPathClass
    {
        public string dbpath()
        {
            string projectDirectory = Directory.GetCurrentDirectory();
            string dbPath = Path.Combine(projectDirectory, "DataBase.db");
            return dbPath;
        }
    }
}
