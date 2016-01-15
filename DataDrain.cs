using System;
using System.Collections.Generic;
using System.Data;

namespace DataDrain.Mapping
{
    internal static class DataDrain
    {
        public static List<T> MapToEntities<T>(this IDbConnection cnn, string sqlConsulta, CommandType tipoComando = CommandType.Text, List<KeyValuePair<string, object>> parametros = null) where T : class, new()
        {
            if (cnn == null)
            {
                throw new ArgumentNullException("cnn", "Conexão não pode ser nula");
            }

            if (string.IsNullOrWhiteSpace(sqlConsulta))
            {
                throw new ArgumentNullException("sqlConsulta", "Consulta não pode ser nula ou vazia");
            }

            var cmd = cnn.CreateCommand();
            cmd.CommandType = tipoComando;
            cmd.CommandText = sqlConsulta;

            if (parametros != null)
            {
                foreach (var param in parametros)
                {
                    cmd.AddParameterWithValue(param.Key, param.Value);
                }
            }

            cnn.Open();

            using (cnn)
            {
                using (var dr = cmd.ExecuteReader())
                {
                    return dr.MapToEntities<T>();
                }
            }
        }

        private static void AddParameterWithValue(this IDbCommand cmd, string nome, object valor)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                throw new ArgumentNullException("nome", "Nome do parametro não pode ser nulo");
            }

            var param = cmd.CreateParameter();
            param.ParameterName = nome;
            param.Value = valor;

            cmd.Parameters.Add(param);
        }
    }
}
