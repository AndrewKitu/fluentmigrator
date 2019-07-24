#region License
//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Model;
using FluentMigrator.Runner.Versioning;
using FluentMigrator.Runner.VersionTableInfo;
using FluentMigrator.Runner.Processors;

using JetBrains.Annotations;

namespace FluentMigrator.Runner
{
    public class VersionLoader : IVersionLoader
    {
        [NotNull]
        private readonly IMigrationProcessor _processor;

        private bool _versionSchemaMigrationAlreadyRun;
        private bool _versionMigrationAlreadyRun;
        private bool _versionUniqueMigrationAlreadyRun;
        private bool _versionDescriptionMigrationAlreadyRun;
        private IVersionInfo _versionInfo;

        public IVersionTableMetaData VersionTableMetaData { get; }

        [NotNull]
        public IMigrationRunner Runner { get; set; }
        public VersionSchemaMigration VersionSchemaMigration { get; }
        public IMigration VersionMigration { get; }
        public IMigration VersionUniqueMigration { get; }
        public IMigration VersionDescriptionMigration { get; }

        public VersionLoader(
            [NotNull] IProcessorAccessor processorAccessor,
            [NotNull] IVersionTableMetaData versionTableMetaData,
            [NotNull] IMigrationRunner runner)
        {
            _processor = processorAccessor.Processor;

            Runner = runner;

            VersionTableMetaData = versionTableMetaData;
            VersionMigration = new VersionMigration(VersionTableMetaData);
            VersionSchemaMigration = new VersionSchemaMigration(VersionTableMetaData);
            VersionUniqueMigration = new VersionUniqueMigration(VersionTableMetaData);
            VersionDescriptionMigration = new VersionDescriptionMigration(VersionTableMetaData);

            LoadVersionInfo();
        }

        public void UpdateVersionInfo(long version)
        {
            UpdateVersionInfo(version, null);
        }

        public void UpdateVersionInfo(long version, string description)
        {
            var dataExpression = new InsertDataExpression();
            dataExpression.Rows.Add(CreateVersionInfoInsertionData(version, description));
            dataExpression.TableName = VersionTableMetaData.TableName;
            dataExpression.SchemaName = VersionTableMetaData.SchemaName;

            dataExpression.ExecuteWith(_processor);
        }

        [NotNull]
        public IVersionTableMetaData GetVersionTableMetaData()
        {
            return VersionTableMetaData;
        }

        protected virtual InsertionDataDefinition CreateVersionInfoInsertionData(long version, string description)
        {
            return new InsertionDataDefinition
                       {
                           new KeyValuePair<string, object>(VersionTableMetaData.ColumnName, version),
                           new KeyValuePair<string, object>(VersionTableMetaData.AppliedOnColumnName, DateTime.UtcNow),
                           new KeyValuePair<string, object>(VersionTableMetaData.DescriptionColumnName, description),
                       };
        }

        public IVersionInfo VersionInfo
        {
            get => _versionInfo;
            set => _versionInfo = value ?? throw new ArgumentException("Cannot set VersionInfo to null");
        }

        public bool AlreadyCreatedVersionSchema => string.IsNullOrEmpty(VersionTableMetaData.SchemaName) ||
            _processor.SchemaExists(VersionTableMetaData.SchemaName);

        public bool AlreadyCreatedVersionTable => _processor.TableExists(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName);

        public bool AlreadyMadeVersionUnique => _processor.ColumnExists(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName, VersionTableMetaData.AppliedOnColumnName);

        public bool AlreadyMadeVersionDescription => _processor.ColumnExists(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName, VersionTableMetaData.DescriptionColumnName);

        public bool OwnsVersionSchema => VersionTableMetaData.OwnsSchema;

        public void LoadVersionInfo()
        {
            if (!AlreadyCreatedVersionSchema && !_versionSchemaMigrationAlreadyRun)
            {
                Runner.Up(VersionSchemaMigration);
                _versionSchemaMigrationAlreadyRun = true;
            }

            if (!AlreadyCreatedVersionTable && !_versionMigrationAlreadyRun)
            {
                Runner.Up(VersionMigration);
                _versionMigrationAlreadyRun = true;
            }

            if (!AlreadyMadeVersionUnique && !_versionUniqueMigrationAlreadyRun)
            {
                Runner.Up(VersionUniqueMigration);
                _versionUniqueMigrationAlreadyRun = true;
            }

            if (!AlreadyMadeVersionDescription && !_versionDescriptionMigrationAlreadyRun)
            {
                Runner.Up(VersionDescriptionMigration);
                _versionDescriptionMigrationAlreadyRun = true;
            }

            _versionInfo = new VersionInfo();

            if (!AlreadyCreatedVersionTable)
            {
                return;
            }

            var dataSet = _processor.ReadTableData(VersionTableMetaData.SchemaName, VersionTableMetaData.TableName);
            var dataTable = dataSet.Tables[0];

            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            var versionColumnIndex = dataTable.Columns.IndexOf(VersionTableMetaData.ColumnName);

            // Find out correct column by case insensitive matching if column was not found. Setting dataTable.caseSensitive = false does not help for some reason.
            if (versionColumnIndex == -1)
            {
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (string.Equals(
                        column.ColumnName,
                        VersionTableMetaData.ColumnName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        versionColumnIndex = dataTable.Columns.IndexOf(column);
                        break;
                    }
                }
            }

            if (versionColumnIndex == -1)
            {
                // The version column couldn't be found.
                var message = new StringBuilder()
                    .AppendFormat(
                        ErrorMessages.VersionColumnNotFound,
                        VersionTableMetaData.ColumnName);
                foreach (DataColumn dataColumn in dataTable.Columns)
                {
                    message.AppendLine().AppendFormat("- {0}", dataColumn.ColumnName);
                }

                throw new InvalidOperationException(message.ToString());
            }

            foreach (DataRow row in dataTable.Rows)
            {
                _versionInfo.AddAppliedMigration(long.Parse(row[versionColumnIndex].ToString()));
            }
        }

        public void RemoveVersionTable()
        {
            var expression = new DeleteTableExpression { TableName = VersionTableMetaData.TableName, SchemaName = VersionTableMetaData.SchemaName };
            expression.ExecuteWith(_processor);

            if (OwnsVersionSchema && !string.IsNullOrEmpty(VersionTableMetaData.SchemaName))
            {
                var schemaExpression = new DeleteSchemaExpression { SchemaName = VersionTableMetaData.SchemaName };
                schemaExpression.ExecuteWith(_processor);
            }
        }

        public void DeleteVersion(long version)
        {
            var expression = new DeleteDataExpression { TableName = VersionTableMetaData.TableName, SchemaName = VersionTableMetaData.SchemaName };
            expression.Rows.Add(new DeletionDataDefinition
                                    {
                                        new KeyValuePair<string, object>(VersionTableMetaData.ColumnName, version)
                                    });
            expression.ExecuteWith(_processor);
        }
    }
}
