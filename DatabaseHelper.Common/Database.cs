using System;
using System.Data;
using System.Data.Common;
using DatabaseHelper.Common.Extensions;

namespace DatabaseHelper.Common
{
    public abstract class Database<TConnection, TCommand, TDataReader> : IDisposable
        where TConnection : class, IDbConnection, new()
        where TCommand : IDbCommand
        where TDataReader : IDataReader
    {
        protected TConnection _connection;
        protected IDbTransaction _transaction;

        protected bool IsTransactionActive => _transaction != null;

        public Database(string connectionString) =>
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

        public string ConnectionString { get; }

        public void BeginTransaction() => _transaction = !IsTransactionActive ?
            OpenConnection().BeginTransaction() :
            throw new InvalidOperationException("There is already active transaction");

        public void Commit(bool forceClose = true) =>
            HandleTransaction(t => t?.Commit(), forceClose);

        public void Rollback(bool forceClose = true) =>
            HandleTransaction(t => t?.Rollback(), forceClose);

        public TConnection GetConnection() =>
            _connection ?? (_connection = new TConnection { ConnectionString = ConnectionString });

        public IDbConnection OpenConnection()
        {
            if (!GetConnection().State.HasFlag(ConnectionState.Open))
            {
                GetConnection().Open();
            }

            return GetConnection();
        }

        public void CloseConnection()
        {
            IDbConnection connection = GetConnection();
            if (connection.State.HasFlag(ConnectionState.Open))
            {
                connection.Close();
            }
        }

        public TCommand GetCommand(string commandText, CommandType commandType, params IDataParameter[] parameters)
        {
            TCommand command = (TCommand) GetConnection().CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            command.Parameters.AddRange(parameters);
            if (_transaction != null) command.Transaction = _transaction;

            return command;
        }

        public TCommand GetCommand(string commandText, params IDataParameter[] parameters) =>
            GetCommand(commandText, CommandType.Text, parameters);

        public int ExecuteNonQuery(string commandText, CommandType commandType, params IDataParameter[] parameters)
        {
            TCommand command = GetCommand(commandText, commandType, parameters);

            try
            {
                OpenConnection();
                return command.ExecuteNonQuery();
            }
            finally
            {
                if (!IsTransactionActive) CloseConnection();
            }
        }

        public int ExecuteNonQuery(string commandText, params IDataParameter[] parameters) =>
            ExecuteNonQuery(commandText, CommandType.Text, parameters);

        public T ExecuteScalar<T>(string commandText, CommandType commandType, params IDataParameter[] parameters)
        {
            TCommand command = GetCommand(commandText, commandType, parameters);

            try
            {
                OpenConnection();
                return (T) Convert.ChangeType(command.ExecuteScalar(), typeof(T));
            }
            finally
            {
                if (!IsTransactionActive) command.Connection.Close();
            }
        }

        public T ExecuteScalar<T>(string commandText, params IDataParameter[] parameters) =>
            ExecuteScalar<T>(commandText, CommandType.Text, parameters);

        public TDataReader GetDataReader(string commandText, CommandType commandType, params IDataParameter[] parameters)
        {
            TCommand command = GetCommand(commandText, commandType, parameters);

            OpenConnection();
            return (TDataReader) command.ExecuteReader();
        }

        public TDataReader GetDataReader(string commandText, params IDataParameter[] parameters) =>
            GetDataReader(commandText, CommandType.Text, parameters);

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        private void HandleTransaction(Action<IDbTransaction> action, bool forceClose)
        {
            if (!IsTransactionActive) throw new InvalidOperationException("There is no active transaction");
            action(_transaction);
            _transaction = null;
            if (forceClose) CloseConnection();
        }
    }
}