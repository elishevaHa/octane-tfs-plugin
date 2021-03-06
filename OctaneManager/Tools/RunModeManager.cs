/*!
* (c) 2016-2018 EntIT Software LLC, a Micro Focus company
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
namespace MicroFocus.Adm.Octane.CiPlugins.Tfs.Core.Tools.Connectivity
{
    public enum PluginRunMode
    {
        ConsoleApp,
        ServerPlugin
    }

    public enum TfsVersionEnum
    {
        Tfs2015,
        Tfs2017,
        Tfs2018
    }

    public class RunModeManager
    {
        private static RunModeManager instance = new RunModeManager();
        private TfsVersionEnum tfsVersion;

        private RunModeManager()
        {
            tfsVersion = TfsVersionEnum.Tfs2018;
#if Package2015
            tfsVersion= TfsVersionEnum.Tfs2015;
#elif Package2017
              tfsVersion= TfsVersionEnum.Tfs2017;
#elif Package2018
              tfsVersion= TfsVersionEnum.Tfs2018;
#endif
        }

        public static RunModeManager GetInstance()
        {
            return instance;
        }

        public PluginRunMode RunMode { get; set; } = PluginRunMode.ServerPlugin;

        public TfsVersionEnum TfsVersion
        {
            get
            {
                return tfsVersion;
            }
        }
        
        public bool RestrictConfigurationAccessFromLocalhost => true;
    }
}
