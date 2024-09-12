using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace CTModel
{
    public class DefectParam
    {
        public int DetailId { get; set; }

        public int RecordId { get; set; }

        [Column("SteelNo")]
        public required string RollNo { get; set; }

        public required string SpotName { get; set; }

        public required string Position { get; set; }

        public required string ImgSavePath { get; set; }

        public required string RemainLength { get; set; }

        public int Type { get; set; }

        public int DefectGrade { get; set; }

        public float RollSpeed { get; set; }

        public float RollWidth { get; set; }

        public float RollThickness { get; set; }

        public DateTime CreateTime { get; set; }

        public int Rect_X { get; set; }

        public int Rect_Y { get; set; }

        public int Rect_W { get; set; }

        public int Rect_H { get; set; }
    }

    public class RollInfo
    {
        public required string RollNo { get; set; }

        public required string RemainLength { get; set; }

        public required string Speed { get; set; }

        public required string Width { get; set; }

        public required string Thickness { get; set; }
    }

    [Table("configurationtable")]
    public class DefectDefineConfig
    {
        public required string MaxValue { get; set; }

        public required string Name { get; set; }
    }

    public class DefectCountReport
    {
        public required string Name { get; set; }

        public required string SpotName { get; set; }

        public int Count { get; set; }
    }

    public class DefectCountDayInMonth
    {
        public DateTime Date { get; set; }

        public required string DefectName { get; set; }

        public required string SpotName { get; set; }

        public int Count { get; set; }
    }
}