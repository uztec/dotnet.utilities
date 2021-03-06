﻿using System;
using System.Collections.Generic;
using System.Data;

namespace UzunTec.Utils.DatabaseAbstraction
{
    internal class QueryExecutionLayerMultipleConnections : IQueryExecutionLayer
    {
        private AbstractionOptions options;
        private QueryPreProccess queryPreProcess;

        public QueryExecutionLayerMultipleConnections(AbstractionOptions options, QueryPreProccess queryPreProcess)
        {
            this.options = options;
            this.queryPreProcess = queryPreProcess;
        }

        public T SafeRunQuery<T>(IDbConnection conn, string queryString, IEnumerable<DataBaseParameter> parameters, Func<IDbCommand, T> executionFunc) where T : class
        {
            return this.SafeRunQuery<T>(conn, queryString, parameters, executionFunc, this.options.AutoCloseConnection);
        }

        public T SafeRunQuery<T>(IDbConnection conn, string queryString, IEnumerable<DataBaseParameter> parameters, Func<IDbCommand, T> executionFunc, bool closeConnection) where T : class
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            T output = null;

            using (IDbCommand command = conn.CreateCommand(queryString, this.queryPreProcess.PreProcessParameters(queryString, parameters)))
            {
                command.CommandText = this.queryPreProcess.PreProcessQuey(command.CommandText);
                output = executionFunc(command);
            }

            if (closeConnection)
            {
                conn.Close();
            }

            return output;
        }
    }
}