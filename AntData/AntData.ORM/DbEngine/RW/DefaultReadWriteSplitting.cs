﻿using System;
using System.Collections.Generic;
using System.Linq;
using AntData.ORM.DbEngine.DB;
using AntData.ORM.Enums;

namespace AntData.ORM.DbEngine.RW
{
    class DefaultReadWriteSplitting : IReadWriteSplitting
    {
        public OperationalDatabases GetOperationalDatabases(Statement statement)
        {
            OperationalDatabases databases = new OperationalDatabases
            {
                OtherCandidates = new List<Database>()
            };

            try
            {
                String databaseSet = statement.DatabaseSet;
                var master = DALBootstrap.DatabaseSets[databaseSet].DatabaseWrappers.Where(item => item.DatabaseType == DatabaseType.Master).Single();
                var slaves = DALBootstrap.DatabaseSets[databaseSet].DatabaseWrappers.Where(item => item.DatabaseType == DatabaseType.Slave)
                    .OrderByDescending(p => p.Ratio).ToList();

                if (slaves != null && slaves.Count > 0)
                {
                    databases.FirstCandidate = slaves[0].Database;
                    slaves.RemoveAt(0);
                    slaves.ForEach(p => databases.OtherCandidates.Add(p.Database));
                    databases.OtherCandidates.Add(master.Database);
                }
            }
            catch
            {
                throw;
            }

            return databases;
        }
    }
}
