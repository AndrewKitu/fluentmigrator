﻿// ***********************************************************************
// Assembly         : FluentMigrator.Tests
// Author           : eivin
// Created          : 10-10-2019
//
// Last Modified By : eivin
// Last Modified On : 10-10-2019
// ***********************************************************************
// <copyright file="AnnouncerTests.cs" company="FluentMigrator Project">
//     Sean Chambers and the FluentMigrator project 2008-2018
// </copyright>
// <summary></summary>
// ***********************************************************************
#region License

// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using FluentMigrator.Runner.Announcers;
using Moq;
using NUnit.Framework;

namespace FluentMigrator.Tests.Unit.Announcers
{
    /// <summary>
    /// Defines test class AnnouncerTests.
    /// </summary>
    [TestFixture]
    [Obsolete]
    public class AnnouncerTests
    {
        /// <summary>
        /// Setups this instance.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            var announcerMock = new Mock<Announcer> {CallBase = true};
            _announcer = announcerMock.Object;
        }

        /// <summary>
        /// The announcer
        /// </summary>
        private Announcer _announcer;

        /// <summary>
        /// Defines the test method ElapsedTime_Should_Not_Write_When_ShowElapsedTime_Is_False.
        /// </summary>
        [Test]
        public void ElapsedTime_Should_Not_Write_When_ShowElapsedTime_Is_False()
        {
            var time = new TimeSpan(0, 1, 40);

            _announcer.ElapsedTime(time);

            Mock.Get(_announcer).Verify(a => a.Write(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }

        /// <summary>
        /// Defines the test method ElapsedTime_Should_Write_When_ShowElapsedTime_Is_True.
        /// </summary>
        [Test]
        public void ElapsedTime_Should_Write_When_ShowElapsedTime_Is_True()
        {
            var time = new TimeSpan(0, 1, 40);
            Mock.Get(_announcer).Setup(a => a.Write("=> 100s", true)).Verifiable();
            _announcer.ShowElapsedTime = true;

            _announcer.ElapsedTime(time);

            Mock.Get(_announcer).VerifyAll();
        }

        /// <summary>
        /// Defines the test method Error_Should_Write.
        /// </summary>
        [Test]
        public void Error_Should_Write()
        {
            var message = "TheMessage";
            Mock.Get(_announcer).Setup(a => a.Write(string.Format("!!! {0}", message), true)).Verifiable();

            _announcer.Error(message);

            Mock.Get(_announcer).VerifyAll();
        }

        /// <summary>
        /// Defines the test method Heading_Should_Write.
        /// </summary>
        [Test]
        public void Heading_Should_Write()
        {
            var message = "TheMessage";
            Mock.Get(_announcer).Setup(a => a.Write(message, true)).Verifiable();

            _announcer.Heading(message);

            Mock.Get(_announcer).VerifyAll();
        }

        /// <summary>
        /// Defines the test method Say_Should_Write.
        /// </summary>
        [Test]
        public void Say_Should_Write()
        {
            var message = "TheMessage";
            Mock.Get(_announcer).Setup(a => a.Write(message, true)).Verifiable();

            _announcer.Say(message);

            Mock.Get(_announcer).VerifyAll();
        }

        /// <summary>
        /// Defines the test method Sql_Should_Not_Write_When_Show_Sql_Is_False.
        /// </summary>
        [Test]
        public void Sql_Should_Not_Write_When_Show_Sql_Is_False()
        {
            var sql = "INSERT INTO table(Id,Name) VALUES (1, 'Test');";

            _announcer.Sql(sql);

            Mock.Get(_announcer).Verify(a => a.Write(It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }

        /// <summary>
        /// Defines the test method Sql_Should_Write_When_Show_Sql_Is_True.
        /// </summary>
        [Test]
        public void Sql_Should_Write_When_Show_Sql_Is_True()
        {
            var sql = "INSERT INTO table(Id,Name) VALUES (1, 'Test');";
            Mock.Get(_announcer).Setup(a => a.Write(sql, false)).Verifiable();
            _announcer.ShowSql = true;

            _announcer.Sql(sql);

            Mock.Get(_announcer).VerifyAll();
        }

        /// <summary>
        /// Defines the test method Sql_Should_Write_When_Show_Sql_Is_True_And_Sql_Is_Empty.
        /// </summary>
        [Test]
        public void Sql_Should_Write_When_Show_Sql_Is_True_And_Sql_Is_Empty()
        {
            var sql = "";
            Mock.Get(_announcer).Setup(a => a.Write("No SQL statement executed.", true)).Verifiable();
            _announcer.ShowSql = true;

            _announcer.Sql(sql);

            Mock.Get(_announcer).VerifyAll();
        }
    }
}
