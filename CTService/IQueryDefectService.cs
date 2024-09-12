using CTClient;
using CTModel;
using Dapper;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.ComponentModel;
using System.Dynamic;
using System.Reflection.Metadata;

namespace CTService
{
    public interface IQueryDefectService
    {
        public DefectParam[] GetRecentDefectList(string spotName, int count);

        public DefectParam[] GetCurrentRollDefectList(string spotName, string rollNo, int count);

        public DefectParam[] GetCurrentDayDefectList(string spotName, int count);

        public DefectParam[] GetDefectByRecordNo(int id);

        /// <summary>
        /// 分页获取缺陷记录
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="parameter"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        public (int TotalCount, DefectParam[] Data) GetPageList(int pageIndex, int pageSize, Dictionary<string, object?> parameter, params string[] filters);

        public DefectParam? GetDefectByDetailId(int id);

        public (Dictionary<string, Dictionary<string, int>> CurrentShift, Dictionary<string, Dictionary<string, int>> CurrentDay, Dictionary<string, Dictionary<string, int>> CurrentMonth) GetDefectCountReport();

        public List<DefectCountDayInMonth> GetDefectDayInMonth();
    }

    public class QueryDefectService : IQueryDefectService
    {
        private readonly string _connection;
        private readonly string _globalSql;
        private readonly IDateRangeService _dateRangeService;

        public QueryDefectService(IOptions<LocalSettings> settings, IDateRangeService dateRangeService) => (_connection, _globalSql, _dateRangeService) = (settings.Value.ConnectionStrings.DefectDb, string.IsNullOrEmpty(settings.Value.DefectSettings.GlobalDefectIgnoreSql) ? string.Empty : settings.Value.DefectSettings.GlobalDefectIgnoreSql, dateRangeService);

        public DefectParam[] GetCurrentDayDefectList(string spotName, int count)
        {
            using var connection = new MySqlConnection(_connection);
            var filters = new[] { _globalSql }.Concat(new[]
            {
                "a.CreateTime >= @MinCurrentDay AND a.CreateTime < @MaxCurrentDay",
                "b.position = @SpotName",
            }).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("WHERE ", string.Join(" AND ", filters)) : string.Empty;
            connection.Open();
            (var min, var max) = _dateRangeService.GetCurrentDayDateRange();
            return connection.Query<DefectParam>($@"SELECT
               a.DetailId,
               a.RecordId,
               b.SteelNo AS RollNo,
               b.position AS SpotName,
               a.Position,
               b.ImgPath AS ImgSavePath,
               a.Type,
               a.RickLevel AS DefectGrade,
               a.CreateTime,
               b.SteelSpeed AS RollSpeed,
               b.SteelWidth AS RollWidth,
               b.SteelThickness AS RollThickness,
               a.Rect_X,
               a.Rect_Y,
               a.Rect_W,
               a.Rect_H
           FROM
               `remindrecorddetail` a
               LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
           {whereSql}
           ORDER BY
               a.CreateTime DESC
               LIMIT @Count", new
            {
                SpotName = spotName,
                MinCurrentDay = min,
                MaxCurrentDay = max,
                Count = count
            }).ToArray();
        }

        public DefectParam[] GetCurrentRollDefectList(string spotName, string rollNo, int count)
        {
            using var connection = new MySqlConnection(_connection);
            var filters = new[] { _globalSql }.Concat(new[]
            {
                "b.position = @SpotName",
                "b.SteelNo = @RollNo"
            }).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("WHERE ", string.Join(" AND ", filters)) : string.Empty;
            connection.Open();

            return connection.Query<DefectParam>($@"SELECT
               a.DetailId,
               a.RecordId,
               b.SteelNo AS RollNo,
               b.position AS SpotName,
               a.Position,
               b.ImgPath AS ImgSavePath,
               a.Type,
               a.RickLevel AS DefectGrade,
               a.CreateTime,
               b.SteelSpeed AS RollSpeed,
               b.SteelWidth AS RollWidth,
               b.SteelThickness AS RollThickness,
               a.Rect_X,
               a.Rect_Y,
               a.Rect_W,
               a.Rect_H
           FROM
               `remindrecorddetail` a
               LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
           {whereSql}
           ORDER BY
               a.CreateTime DESC
               LIMIT @Count", new
            {
                SpotName = spotName,
                RollNo = rollNo,
                Count = count
            }).ToArray();
        }

        public DefectParam? GetDefectByDetailId(int id)
        {
            using var connection = new MySqlConnection(_connection);
            var filters = new[] { _globalSql }.Concat(new[]
            {
                "a.DetailId = @DetailId"
            }).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("WHERE ", string.Join(" AND ", filters)) : string.Empty;
            connection.Open();

            return connection.QueryFirstOrDefault<DefectParam>($@"SELECT
                a.DetailId,
                a.RecordId,
                b.SteelNo AS RollNo,
                b.position AS SpotName,
                a.Position,
                b.ImgPath AS ImgSavePath,
                a.Type,
                a.RickLevel AS DefectGrade,
                a.CreateTime,
                b.SteelSpeed AS RollSpeed,
                b.SteelWidth AS RollWidth,
                b.SteelThickness AS RollThickness,
                a.Rect_X,
                a.Rect_Y,
                a.Rect_W,
                a.Rect_H
            FROM
                `remindrecorddetail` a
                LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
            {whereSql}
            ORDER BY
                a.CreateTime DESC", new
            {
                DetailId = id
            });
        }

        public DefectParam[] GetDefectByRecordNo(int id)
        {
            using var connection = new MySqlConnection(_connection);
            var filters = new[] { _globalSql }.Concat(new[]
            {
                "b.RecordNo = @RecordNo"
            }).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("WHERE ", string.Join(" AND ", filters)) : string.Empty;
            connection.Open();

            return connection.Query<DefectParam>($@"SELECT
                a.DetailId,
                a.RecordId,
                b.SteelNo AS RollNo,
                b.position AS SpotName,
                a.Position,
                b.ImgPath AS ImgSavePath,
                a.Type,
                a.RickLevel AS DefectGrade,
                a.CreateTime,
                b.SteelSpeed AS RollSpeed,
                b.SteelWidth AS RollWidth,
                b.SteelThickness AS RollThickness,
                a.Rect_X,
                a.Rect_Y,
                a.Rect_W,
                a.Rect_H
            FROM
                `remindrecorddetail` a
                LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
            {whereSql}
            ORDER BY
                a.CreateTime DESC", new
            {
                RecordNo = id
            }).ToArray();
        }

        public DefectParam[] GetRecentDefectList(string spotName, int count)
        {
            using var connection = new MySqlConnection(_connection);
            var filters = new[] { _globalSql }.Concat(new[]
            {
                "b.position = @SpotName"
            }).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("WHERE ", string.Join(" AND ", filters)) : string.Empty;
            connection.Open();

            return connection.Query<DefectParam>($@"SELECT
                a.DetailId,
                a.RecordId,
                b.SteelNo AS RollNo,
                b.position AS SpotName,
                a.Position,
                b.ImgPath AS ImgSavePath,
                a.Type,
                a.RickLevel AS DefectGrade,
                a.CreateTime,
                b.SteelSpeed AS RollSpeed,
                b.SteelWidth AS RollWidth,
                b.SteelThickness AS RollThickness,
                a.Rect_X,
                a.Rect_Y,
                a.Rect_W,
                a.Rect_H
            FROM
                `remindrecorddetail` a
                LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
            {whereSql}
            ORDER BY
                a.CreateTime DESC
                LIMIT @Count", new
            {
                SpotName = spotName,
                Count = count
            }).ToArray();
        }

        public (int TotalCount, DefectParam[] Data) GetPageList(int pageIndex, int pageSize, Dictionary<string, object?> parameter, params string[] filterArr)
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();
            var filters = filterArr.Append(_globalSql).Where(x => !string.IsNullOrEmpty(x));
            var whereSql = filters.Any() ? string.Concat("HAVING ", string.Join(" AND ", filters)) : string.Empty;

            var sql = $@"
            SELECT COUNT(1) FROM
            (SELECT
                a.DetailId AS {nameof(DefectParam.DetailId)},
                a.RecordId AS {nameof(DefectParam.RecordId)},
                b.SteelNo AS {nameof(DefectParam.RollNo)},
                b.position AS {nameof(DefectParam.SpotName)},
                a.Position AS {nameof(DefectParam.Position)},
                b.ImgPath AS {nameof(DefectParam.ImgSavePath)},
                a.Type AS {nameof(DefectParam.Type)},
                a.RickLevel AS {nameof(DefectParam.DefectGrade)},
                a.CreateTime AS {nameof(DefectParam.CreateTime)},
                b.SteelSpeed AS {nameof(DefectParam.RollSpeed)},
                b.SteelWidth AS {nameof(DefectParam.RollWidth)},
                b.SteelThickness AS {nameof(DefectParam.RollThickness)},
                b.RemainingLength AS {nameof(DefectParam.RemainLength)},
                a.Rect_X AS {nameof(DefectParam.Rect_X)},
                a.Rect_Y AS {nameof(DefectParam.Rect_Y)},
                a.Rect_W AS {nameof(DefectParam.Rect_W)},
                a.Rect_H AS {nameof(DefectParam.Rect_H)}
            FROM
                `remindrecorddetail` a
            LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
            {whereSql}) t;

            SELECT
                a.DetailId AS {nameof(DefectParam.DetailId)},
                a.RecordId AS {nameof(DefectParam.RecordId)},
                b.SteelNo AS {nameof(DefectParam.RollNo)},
                b.position AS {nameof(DefectParam.SpotName)},
                a.Position AS {nameof(DefectParam.Position)},
                b.ImgPath AS {nameof(DefectParam.ImgSavePath)},
                a.Type AS {nameof(DefectParam.Type)},
                b.RemainingLength AS {nameof(DefectParam.RemainLength)},
                a.RickLevel AS {nameof(DefectParam.DefectGrade)},
                a.CreateTime AS {nameof(DefectParam.CreateTime)},
                b.SteelSpeed AS {nameof(DefectParam.RollSpeed)},
                b.SteelWidth AS {nameof(DefectParam.RollWidth)},
                b.SteelThickness AS {nameof(DefectParam.RollThickness)},
                a.Rect_X AS {nameof(DefectParam.Rect_X)},
                a.Rect_Y AS {nameof(DefectParam.Rect_Y)},
                a.Rect_W AS {nameof(DefectParam.Rect_W)},
                a.Rect_H AS {nameof(DefectParam.Rect_H)}
            FROM
                `remindrecorddetail` a
            LEFT JOIN `remindrecord` b ON a.RecordId = b.RecordNo
            {whereSql}
            ORDER BY
                a.CreateTime DESC
            LIMIT {pageSize} OFFSET {pageSize * pageIndex};";
            using var multi = connection.QueryMultiple(sql, parameter);
            return (multi.ReadSingle<int>(), multi.Read<DefectParam>().ToArray());
        }

        public (Dictionary<string, Dictionary<string, int>> CurrentShift, Dictionary<string, Dictionary<string, int>> CurrentDay, Dictionary<string, Dictionary<string, int>> CurrentMonth) GetDefectCountReport()
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();

            var sql = $@"
                SELECT
                CASE
                    WHEN
                        a.Type = 0
                        AND a.RickLevel = 0 THEN
                            '轻微边裂'
                            WHEN a.Type = 0
                            AND a.RickLevel = 1 THEN
                                '中等边裂'
                                WHEN a.Type = 0
                                AND a.RickLevel = 2 THEN
                                    '严重边裂'
                                    WHEN a.Type = 1 THEN
                                    '折印' ELSE '未知'
                                END AS `Name`,
                                COUNT(*) AS `Count`,
                                b.position AS SpotName
                            FROM
                                remindrecorddetail a
                            LEFT JOIN remindrecord b on a.RecordId = b.RecordNo
                            WHERE a.`CreateTime` >= @MinShiftTime AND a.`CreateTime` < @MaxShiftTime
                        GROUP BY
                    `Name`, `SpotName`;

                SELECT
                CASE
                    WHEN
                        a.Type = 0
                        AND a.RickLevel = 0 THEN
                            '轻微边裂'
                            WHEN a.Type = 0
                            AND a.RickLevel = 1 THEN
                                '中等边裂'
                                WHEN a.Type = 0
                                AND a.RickLevel = 2 THEN
                                    '严重边裂'
                                    WHEN a.Type = 1 THEN
                                    '折印' ELSE '未知'
                                END AS `Name`,
                                COUNT(*) AS `Count`,
                                b.position AS SpotName
                            FROM
                                remindrecorddetail a
                            LEFT JOIN remindrecord b on a.RecordId = b.RecordNo
                            WHERE a.`CreateTime` >= @MinDayTime AND a.`CreateTime` < @MaxDayTime
                        GROUP BY
                    `Name`, `SpotName`;

                 SELECT
                CASE
                    WHEN
                        a.Type = 0
                        AND a.RickLevel = 0 THEN
                            '轻微边裂'
                            WHEN a.Type = 0
                            AND a.RickLevel = 1 THEN
                                '中等边裂'
                                WHEN a.Type = 0
                                AND a.RickLevel = 2 THEN
                                    '严重边裂'
                                    WHEN a.Type = 1 THEN
                                    '折印' ELSE '未知'
                                END AS `Name`,
                                COUNT(*) AS `Count`,
                                b.position AS SpotName
                            FROM
                                remindrecorddetail a
                            LEFT JOIN remindrecord b on a.RecordId = b.RecordNo
                            WHERE a.`CreateTime` >= @MinMonthTime AND a.`CreateTime` < @MaxMonthTime
                        GROUP BY
                    `Name`, `SpotName`;

            ";
            var shiftDateRange = _dateRangeService.GetCurrentShiftDateRange();
            var dayDateRange = _dateRangeService.GetCurrentDayDateRange();
            var monthDateRange = _dateRangeService.GetCurrentMonthDateRange();
            using var multi = connection.QueryMultiple(sql, new
            {
                MinShiftTime = shiftDateRange.ShiftBegin,
                MaxShiftTime = shiftDateRange.ShiftEnd,
                MinDayTime = dayDateRange.DayBegin,
                MaxDayTime = dayDateRange.DayEnd,
                MinMonthTime = monthDateRange.MonthBegin,
                MaxMonthTime = monthDateRange.MonthEnd,
            });

            return (multi.Read<DefectCountReport>().ToList().GroupBy(x => x.SpotName).ToDictionary(x => x.Key, x => x.ToDictionary(_ => _.Name, _ => _.Count)), multi.Read<DefectCountReport>().GroupBy(x => x.SpotName).ToDictionary(x => x.Key, x => x.ToDictionary(_ => _.Name, _ => _.Count)), multi.Read<DefectCountReport>().GroupBy(x => x.SpotName).ToDictionary(x => x.Key, x => x.ToDictionary(_ => _.Name, _ => _.Count)));
        }

        public List<DefectCountDayInMonth> GetDefectDayInMonth()
        {
            using var connection = new MySqlConnection(_connection);

            connection.Open();
            var sql = $@"
                SELECT
                DATE ( a.CreateTime ) AS `Date`,
                    CASE
                    WHEN
                        a.Type = 0
                        AND a.RickLevel = 0 THEN
                            '轻微边裂'
                            WHEN a.Type = 0
                            AND a.RickLevel = 1 THEN
                                '中等边裂'
                                WHEN a.Type = 0
                                AND a.RickLevel = 2 THEN
                                    '严重边裂'
                                    WHEN a.Type = 1 THEN
                                    '折印' ELSE '未知'
                                END AS `DefectName`,
                                COUNT(*) AS `Count`,
                                b.position AS SpotName

            FROM
                remindrecorddetail a
            LEFT JOIN remindrecord b on a.RecordId = b.RecordNo
            WHERE a.`CreateTime` >= @MinMonthTime AND a.`CreateTime` < @MaxMonthTime
            GROUP BY
                `Date`, `DefectName`, `SpotName`";

            var monthDateRange = _dateRangeService.GetCurrentMonthDateRange();

            return connection.Query<DefectCountDayInMonth>(sql, new
            {
                MinMonthTime = monthDateRange.MonthBegin,
                MaxMonthTime = monthDateRange.MonthEnd
            }).ToList();
        }
    }
}