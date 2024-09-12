using CTClient;
using CTModel;
using Dapper;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTService
{
    public interface IThresholdService
    {
        public IList<DefectDefineConfig> GetDefectDefineConfigs();

        public void UpdateConfigs(List<DefectDefineConfig> configs);
    }

    public class ThresholdService : IThresholdService
    {
        private readonly string _connection;

        public ThresholdService(IOptions<LocalSettings> settings) => (_connection) = (settings.Value.ConnectionStrings.DefectDb);

        public IList<DefectDefineConfig> GetDefectDefineConfigs()
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();

            return connection.Query<DefectDefineConfig>(@"
                SELECT
                    ConfigurationName AS `Name`,
                    ConfigurationMax AS `MaxValue`,
                    ConfigurationMin AS `MinValue`,
                    IsActivate AS `IsActivate`
                FROM
                    configurationtable").ToList();
        }

        public void UpdateConfigs(List<DefectDefineConfig> configs)
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();
            connection.Execute(string.Join("", configs.Select(x => $@"
                UPDATE configurationtable
                SET ConfigurationMax = {x.MaxValue},
                ConfigurationMin = {x.MinValue},
                IsActivate = {x.IsActivate}
                WHERE
                    ConfigurationName = '{x.Name}';")));
        }
    }
}