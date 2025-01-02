using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Simple.Reflection;

namespace Simple.Data
{
    public static class DataHelper
    {
        public static DateTime DateTimeParameterValue(DateTime dateTime)
        {
            return dateTime.ToUniversalTime();
        }

        public static object DateTimeParameterValue(DateTime? dateTime)
        {
            return DataHelper.ValueOrDBNull(dateTime.HasValue ? DateTimeParameterValue(dateTime.Value) : default(DateTime?));
        }

        public static DateTime DateAsUtc(DateTime source)
        {
            return DateTime.SpecifyKind(source, DateTimeKind.Utc);
        }
        public static DateTime? DateAsUtc(DateTime? source)
        {
            var result = default(DateTime?);
            if (source.HasValue)
            {
                result = DateAsUtc(source.Value);
            }
            return result;
        }

      
        /// <summary>
        /// Get safe value, if value not exists (DBNull) return the default(EntityName)
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TType GetSafeValue<TType>(object value)
        {
            return GetSafeValue(value, default(TType));
        }

        /// <summary>
        /// Get safe value, if value not exists (DBNull) return the default value
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TType GetSafeValue<TType>(object value, TType defaultValue)
        {
            if (value == null || Convert.IsDBNull(value))
            {
                return defaultValue;
            }
            else
            {
                if (value is DateTime)
                {
                    value = ((DateTime)value).ToUniversalTime().ToLocalTime();
                }


                return (TType)value;
            }
        }

        /// <summary>
        /// Get string value or an empty string if value not exists
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetStringValueOrEmptyString(object value)
        {
            return GetSafeValue<string>(value, string.Empty);
        }

        public static void TraceDataReader(DbDataReader reader)
        {
            int resultSet = 0;
            do
            {
                Trace.WriteLine("---------------------------------------------------------");
                Trace.WriteLine(resultSet);
                Trace.WriteLine("---------------------------------------------------------");

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Trace.Write(reader.GetName(i));
                        Trace.Write(" , ");
                    }
                    Trace.WriteLine("");
                }
                resultSet++;
            } while (reader.NextResult());
        }

        public static void AddParameter(System.Data.Common.DbCommand command, string paramName, System.Data.DbType dbType)
        {
            AddParameter(command, paramName, dbType, 0);
        }

        public static void AddParameter(System.Data.Common.DbCommand command, string paramName, System.Data.DbType dbType, int size)
        {
            AddParameter(command, paramName, dbType, size, ParameterDirection.Input);
        }

        public static void AddParameter(System.Data.Common.DbCommand command, string paramName, System.Data.DbType dbType, int size, ParameterDirection direction)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = paramName;
            parameter.DbType = dbType;
            parameter.Size = size;
            parameter.Direction = direction;
            command.Parameters.Add(parameter);
        }

        public static void AddParameterWithValue(System.Data.Common.DbCommand command, string paramName, System.Data.DbType dbType, int size, ParameterDirection direction, object value, bool handleDBNull)
        {
            if (handleDBNull)
            {
                value = ValueOrDBNull(value);
            }
            AddParameterWithValue(command, paramName, dbType, size, direction, value);
        }


        public static void AddParameterWithValue(System.Data.Common.DbCommand command, string paramName, DbType dbType, object value, ParameterDirection direction = ParameterDirection.Input)
        {
            AddParameterWithValue(command, paramName, dbType, 0, direction, value);
        }

        public static void AddTableParameter(DbCommand command, string parameterName, string typeName, DataTable value)
        {
            var parameter = new SqlParameter(parameterName, SqlDbType.Structured);

            parameter.TypeName = typeName;
            parameter.Value = value;

            command.Parameters.Add(parameter);
        }

        public static void AddParameterWithValue(System.Data.Common.DbCommand command, string paramName, System.Data.DbType dbType, int size, ParameterDirection direction, object value)
        {
            AddParameter(command, paramName, dbType, size, direction);
            var parameter = command.Parameters[paramName];
            parameter.Value = value;
        }

        public static string GetLikeString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            return string.Format("%{0}%", text);
        }

        public static object StringValueOrDBNull(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DBNull.Value;
            }
            else
            {
                return value;
            }
        }

        public static object ValueOrDBNull(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            else
            {
                return value;
            }
        }
    }
    public interface IDataReaderMapper
    {
        void Read(DbDataReader reader);
    }
    public class FieldMappingContext
    {
        public bool Mapped { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }
        public string DataTypeName { get; set; }
    }
    public class DbReaderMapper<T> : IDataReaderMapper
    {
        private readonly Func<T> m_create;


        public DbReaderMapper()
            : this(Activator.CreateInstance<T>)
        {
        }

        public DbReaderMapper(Func<T> create)
        {
            m_create = create;
           
        }

        
        public List<T> Results { get; set; }

        protected virtual object MapField(string name, string dataTypeName, Type propertyType, DbDataReader reader)
        {

            var value = DataHelper.GetSafeValue(reader[name], default(object));

                    if (propertyType == typeof(DateTime) && value != null)
                    {
                        value = DataHelper.DateAsUtc((DateTime)value);
                    }

                    if (propertyType == typeof(DateTime?))
                    {
                        value = DataHelper.DateAsUtc((DateTime?)value);
                    }

       if (propertyType.IsArray)
                    {
                        if (propertyType.GetElementType() == typeof(string))
                        {
                            if (value == null)
                            {
                                value = new string[] { };
                            }
                            else
                            {
                                value = (value as string).Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
                            }
                        }
                    }
                    else if (propertyType.IsEnum || (propertyType.IsNullable() && propertyType.UnderlyingType().IsEnum))
                    {
                        if (value != null)
                        {
                            value = Convert.ChangeType(
                                Enum.ToObject(propertyType.UnderlyingType(), value),
                                propertyType.UnderlyingType());
                        }
                    }


                return value;
        
        }

        public void Read(DbDataReader reader)
        {
            var items = new List<T>();
            var schema = reader.GetSchemaTable();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertiesMap = new Dictionary<string, PropertyInfo>();
            foreach (var property in properties)
            {
                propertiesMap[property.Name] = property;
            }
            while (reader.Read())
            {
                var item = m_create();
                ReadRow(reader, item, schema, propertiesMap);

                items.Add(item);
            }

            Results = items;
        }

        public void ReadRow(DbDataReader reader, T item, DataTable schema = default(DataTable), Dictionary<string, PropertyInfo> propertiesMap = default(Dictionary<string, PropertyInfo>))
        {
            schema = schema ?? reader.GetSchemaTable();
            if (propertiesMap == null)
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertiesMap = new Dictionary<string, PropertyInfo>();
                foreach (var property in properties)
                {
                    propertiesMap[property.Name] = property;
                }
            }


            foreach (var column in schema.Rows.Cast<DataRow>())
            {
                var name = column["ColumnName"] as string;
                var dataTypeName = column["DataTypeName"] as string;
                var innerName = string.Empty;
                if (name.Contains("."))
                {
                    innerName = name.Split('.').Last();
                    name = name.Split('.').First();
                }

                PropertyInfo property;


                if (propertiesMap.TryGetValue(name, out property) &&
                    property.CanWrite)
                {
                    var target = default(object);
                    var targetInnerProperty = default(PropertyInfo);
                    if (!string.IsNullOrEmpty(innerName))
                    {
                        target = property.GetValue(item);
                        targetInnerProperty = property;
                        property = property.PropertyType.GetProperty(innerName);
                    }

                    if (!string.IsNullOrEmpty(innerName))
                    {
                        name = name + "." + innerName;
                    }

                    if (property != null)
                    {
                        var value = MapField(name, dataTypeName, property.PropertyType, reader);
                        if (value != null)
                        {
                            if (targetInnerProperty != null)
                            {
                                if (target == null)
                                {
                                    target = Activator.CreateInstance(targetInnerProperty.PropertyType);
                                    targetInnerProperty.SetValue(item, target);
                                }
                            }
                            property.SetValue(target ?? item, value);
                        }
                    }
                }
            }


        }
    }
}