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
        private readonly string _globalSql;

        public ThresholdService(IOptions<LocalSettings> settings) => (_connection, _globalSql) = (settings.Value.ConnectionStrings.DefectDb, string.IsNullOrEmpty(settings.Value.DefectSettings.GlobalDefectIgnoreSql) ? string.Empty : settings.Value.DefectSettings.GlobalDefectIgnoreSql);

        public IList<DefectDefineConfig> GetDefectDefineConfigs()
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();

            return connection.Query<DefectDefineConfig>(@"
                SELECT
                    ConfigurationName AS `Name`,
                    ConfigurationMax AS `MaxValue`
                FROM
                    configurationtable").ToList();
        }

        public void UpdateConfigs(List<DefectDefineConfig> configs)
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();
            connection.Execute(string.Join("", configs.Select(x => $@"
                UPDATE configurationtable
                SET ConfigurationMax = {x.MaxValue}
                WHERE
                    ConfigurationName = '{x.Name}';")));
        }
    }
}