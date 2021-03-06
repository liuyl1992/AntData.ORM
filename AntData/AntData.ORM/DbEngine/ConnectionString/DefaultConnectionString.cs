﻿
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Xml;
using System.Linq;
using AntData.ORM.Common.Util;
using AntData.ORM.Dao;

namespace AntData.ORM.DbEngine.ConnectionString
{
    /// <summary>
    /// 默认的获取连接字符串的方式 从config文件里面获取
    /// 从config文件里面读取到对应的逻辑数据库名称 然后在去 config文件里面配置的地址去获取对应的connectionString
    /// </summary>
    class DefaultConnectionString : IConnectionString
    {
        private static readonly ConnectionStringSettingsCollection connectionStringCollection;
        private static readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        static DefaultConnectionString()
        {
            try
            {
                var collection = DALBootstrap.ConnectionStringKeys;
                if (collection == null || collection.Count < 1)
                {
                    throw new DalException("Missing DataConnection.ConnectionStrings.");
                }
                //这里原来的意思是从一个固定的地址去根据逻辑数据库名(key)去读真实的连接字符串
                String path = DALBootstrap.GetConnectionLocatorPath();
                if (String.IsNullOrEmpty(path))
                {
                    //默认是从config文件里面去读取
                    connectionStringCollection = loadFronBootstrapConfig(collection);
                    return;
                }

                //从指定的路径文件里面读取的
                rwLock.EnterWriteLock();
                connectionStringCollection = getConnectionStrings(collection.AllKeys, path);
                if (connectionStringCollection == null)
                    connectionStringCollection = new ConnectionStringSettingsCollection();
                rwLock.ExitWriteLock();
            }
            catch
            {
                throw;
            }
        }

        private static ConnectionStringSettingsCollection loadFronBootstrapConfig(NameValueCollection ConnectionStringKeys)
        {
            var collection = new ConnectionStringSettingsCollection();
            foreach (KeyValuePair<String, String> connectionStringKeyValue in ConnectionStringKeys.AsKVP())
            {
                try
                {
                    collection.Add(new ConnectionStringSettings
                    {
                        Name = connectionStringKeyValue.Key,
                        ConnectionString = connectionStringKeyValue.Value
                    });
                }
                catch 
                {
                    //ignore
                }
            }
            return collection;
        }

        public ConnectionStringSettings GetConnectionString(String key)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentException("GetConnectionString：The value can not be null or an empty string.", "key");

            try
            {
                rwLock.EnterReadLock();
                ConnectionStringSettings connectionString = connectionStringCollection[key];
                rwLock.ExitReadLock();
                return connectionString;
            }
            catch
            {
                return new ConnectionStringSettings();
            }
        }

        /// <summary>
        /// 取得指定的连接字符串
        /// </summary>
        /// <param name="array">字符串名称，不区分大小写</param>
        /// <returns></returns>
        private static ConnectionStringSettingsCollection getConnectionStrings(String[] array, String configPath)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            var collection = new ConnectionStringSettingsCollection();
            if (array.Length == 0)
                return collection;

            var keys = new List<String>(array).Select(r=>r.ToLower()).ToList();
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };

            try
            {
                using (var stream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "add")
                            {
                                String name = reader.GetAttribute("name");
                                if (name != null)
                                {
                                    Int32 index = keys.IndexOf(name.ToLower());
                                    if (index > -1)
                                    {
                                        collection.Add(fetchConnectionString(name, reader));
                                        keys.RemoveAt(index);
                                        if (keys.Count == 0) break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            return collection;
        }

        private static ConnectionStringSettings fetchConnectionString(String name, XmlReader reader)
        {
            String connectionString = reader.GetAttribute("connectionString");
            validateRequired("connectionString", connectionString, name);

            //String provName = reader.GetAttribute("providerName");
            //validateRequired("providerName", provName, name);

            return new ConnectionStringSettings
            {
                Name = name,
                ConnectionString = connectionString,
                //ProviderName = provName
            };
        }

        private static void validateRequired(String attrName, String attrValue, String stringName)
        {
            if (String.IsNullOrEmpty(attrValue))
                throw new ConfigurationErrorsException(String.Format("Connection string '{0}' configuration error, required attribute '{1}' is not found or the value for it is not valid.", stringName, attrName));
        }
    }
}

