﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyEPlanner.PxcIolinkConfiguration.Models.IolConf;

namespace EasyEPlanner.PxcIolinkConfiguration
{
    public interface IXmlTemplateReader
    {
        /// <summary>
        /// Возвращает список тасок с чтением шаблонов в dataStore. Таски
        /// уже запущены.
        /// </summary>
        /// <param name="pathToFolder">Путь к каталогу с шаблонами</param>
        /// <param name="dataStore">Хранилище, куда будут читаться шаблоны</param>
        /// <returns></returns>
        List<Task> Read(string pathToFolder,
            Dictionary<string, LinerecorderSensor> dataStore);

        /// <summary>
        /// Возвращает версию шаблона из первого считанного файла-шаблона.
        /// </summary>
        string TemplateVersion { get; }
    }

    /// <summary>
    /// Класс с логикой чтения шаблонов Xml с описанием модулей ввода-вывода и
    /// устройств.
    /// </summary>
    internal class XmlTemplateReader : IXmlTemplateReader
    {
        private const string lrpFileSearchPattern = "*.lrp";
        private IXmlSensorSerializer sensorSerializer;

        public XmlTemplateReader(IXmlSensorSerializer sensorSerializer)
        {
            this.sensorSerializer = sensorSerializer;
        }

        public List<Task> Read(string pathToFolder,
            Dictionary<string, LinerecorderSensor> dataStore)
        {
            var tasks = new List<Task>();
            var directoryInfo = new DirectoryInfo(pathToFolder);
            var paths = directoryInfo
                .GetFiles(lrpFileSearchPattern)
                .Select(x => new
                {
                    Path = x.FullName,
                    TemplateName = x.Name.Substring(0, x.Name.Length - x.Extension.Length)
                });
            foreach (var fileInfo in paths)
            {
                var task = Task
                    .Run(() => ReadTemplate(fileInfo.Path, fileInfo.TemplateName, dataStore));
                tasks.Add(task);
            }

            return tasks;
        }

        private void ReadTemplate(string path, string templateName,
            Dictionary<string, LinerecorderSensor> store)
        {
            string xml = File.ReadAllText(path, Encoding.UTF8);
            LinerecorderSensor recorder;
            try
            {
                recorder = sensorSerializer.Deserialize(xml);
            }
            catch (Exception ex)
            {
                throw new Exception($"Шаблон {path} с ошибкой.",
                    ex.InnerException);
            }

            lock (store)
            {
                if (store.ContainsKey(templateName))
                {
                    throw new InvalidDataException(
                        $"Шаблон {templateName} уже существует.");
                }

                store.Add(templateName, recorder);

                if (string.IsNullOrEmpty(TemplateVersion))
                {
                    TemplateVersion = recorder.Version;
                }
            }
        }

        public string TemplateVersion { get; set; }
    }
}
