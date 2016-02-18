using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DataDrain.Caching;


namespace DataDrain.Mapping
{
    internal static class Map
    {
        private static readonly CachingMannager Cache = new CachingMannager(new TimeSpan(0, 0, 5, 0));


        /// <summary>
        /// Mapeia os campos do DataTable para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dt">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this DataTable dt) where T : class, new()
        {
            var dr = dt.CreateDataReader();

            return dr.MapToEntities<T>();
        }

        /// <summary>
        /// Mapeia os campos do DataView para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dv">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this DataView dv) where T : class, new()
        {
            var dt = dv.ToTable("Tabela");

            return dt.MapToEntities<T>();
        }

        /// <summary>
        /// Mapeia os campos do dataReader para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dr">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this IDataReader dr) where T : class ,new()
        {
            try
            {
                var listaNovosObjetos = new List<T>();
                var camposValidos = RetornaMapObjeto<T>(dr);

                var setters = RetornaSetters<T>(camposValidos);

                while (dr.Read())
                {
                    var novoObjeto = new T();

                    foreach (var p in camposValidos)
                    {
                        var valorDr = dr[p.Name];

                        var setter = setters[p.Name];

                        if (valorDr != DBNull.Value)
                        {
                            if (valorDr is TimeSpan && p.PropertyType == typeof(DateTime))
                            {

                                setter(novoObjeto, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (valorDr is byte[] && p.PropertyType == typeof(string))
                            {
                                setter(novoObjeto, Convert.ChangeType(System.Text.Encoding.Default.GetString((byte[])valorDr), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (valorDr is Guid && p.PropertyType == typeof(string))
                            {
                                setter(novoObjeto, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && p.PropertyType.GetGenericArguments()[0].IsEnum)
                            {
                                setter(novoObjeto, Enum.Parse(p.PropertyType.GetGenericArguments()[0], valorDr.ToString()));
                            }
                            else if (!p.PropertyType.IsEnum)
                            {
                                setter(novoObjeto, Nullable.GetUnderlyingType(p.PropertyType) != null
                                        ? Convert.ChangeType(valorDr, Nullable.GetUnderlyingType(p.PropertyType), System.Globalization.CultureInfo.InvariantCulture)
                                        : Convert.ChangeType(valorDr, p.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (p.PropertyType.IsEnum)
                            {
                                if (valorDr is string)
                                {
                                    setter(novoObjeto, Enum.Parse(p.PropertyType, valorDr.ToString()));
                                }
                                else
                                {
                                    setter(novoObjeto, Enum.ToObject(p.PropertyType, valorDr));
                                }
                            }
                        }
                        else
                        {
                            if ((p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                            {
                                setter(novoObjeto, null);
                            }
                            else
                            {
                                setter(novoObjeto, !p.PropertyType.IsValueType
                                    ? null
                                    : Activator.CreateInstance(p.PropertyType));
                            }
                        }
                    }
                    listaNovosObjetos.Add(novoObjeto);
                }

                return listaNovosObjetos;

            }
            catch
            {
                throw new Exception();
            }
        }


        private static Dictionary<string, Action<T, object>> RetornaSetters<T>(IEnumerable<PropertyInfo> camposValidos)
        {
            var map = Cache.Recuperar<Dictionary<string, Dictionary<string, Action<T, object>>>>(typeof(T).FullName).Value ?? new Dictionary<string, Dictionary<string, Action<T, object>>>();

            var mapObjAtual = camposValidos.ToDictionary(camposValido => camposValido.Name, FastInvoke.BuildUntypedSetter<T>);

            if (map.ContainsKey(typeof(T).FullName))
            {
                return map[typeof(T).FullName];
            }

            map.Add(typeof(T).FullName, mapObjAtual);
            Cache.Adicionar(typeof(T).FullName, map);
            return mapObjAtual;
        }

        private static List<PropertyInfo> RetornaMapObjeto<T>(IDataRecord dr) where T : new()
        {
            if (dr != null)
            {
                return typeof(T).GetProperties().Where(p => p.CanWrite && dr.HasColumn(p.Name)).ToList();
            }

            throw new ArgumentNullException("dr", "DataReader não pode ser nulo");
        }

        private static bool HasColumn(this IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
