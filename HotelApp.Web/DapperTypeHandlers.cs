using System.Data;
using Dapper;

namespace HotelApp.Web
{
    /// <summary>
    /// Dapper TypeHandler for DateOnly ↔ SQL DATE.
    /// Registered once in Program.cs so all repositories handle DateOnly parameters
    /// and result-set mapping automatically.
    /// </summary>
    public class DateOnlyDapperHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value  = value.ToDateTime(TimeOnly.MinValue);
        }

        public override DateOnly Parse(object value)
        {
            return value switch
            {
                DateTime dt => DateOnly.FromDateTime(dt),
                DateOnly   d => d,
                _            => DateOnly.FromDateTime(Convert.ToDateTime(value))
            };
        }
    }

    public class NullableDateOnlyDapperHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value  = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        }

        public override DateOnly? Parse(object value)
        {
            if (value is DBNull || value is null) return null;
            return value switch
            {
                DateTime dt => DateOnly.FromDateTime(dt),
                DateOnly   d => d,
                _            => DateOnly.FromDateTime(Convert.ToDateTime(value))
            };
        }
    }

    /// <summary>
    /// Dapper TypeHandler for TimeOnly ↔ SQL TIME.
    /// </summary>
    public class TimeOnlyDapperHandler : SqlMapper.TypeHandler<TimeOnly>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value  = value.ToTimeSpan();
        }

        public override TimeOnly Parse(object value)
        {
            return value switch
            {
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                TimeOnly t  => t,
                DateTime dt => TimeOnly.FromDateTime(dt),
                _           => TimeOnly.FromTimeSpan((TimeSpan)Convert.ChangeType(value, typeof(TimeSpan)))
            };
        }
    }

    public class NullableTimeOnlyDapperHandler : SqlMapper.TypeHandler<TimeOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
        {
            parameter.DbType = DbType.Time;
            parameter.Value  = value.HasValue ? value.Value.ToTimeSpan() : DBNull.Value;
        }

        public override TimeOnly? Parse(object value)
        {
            if (value is DBNull || value is null) return null;
            return value switch
            {
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                TimeOnly t  => t,
                DateTime dt => TimeOnly.FromDateTime(dt),
                _           => TimeOnly.FromTimeSpan((TimeSpan)Convert.ChangeType(value, typeof(TimeSpan)))
            };
        }
    }
}
