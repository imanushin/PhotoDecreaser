using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhotoDecreaser.UpdateService;
using System.Reflection;
using RunScriptLibrary;
using System.Diagnostics;

namespace PhotoDecreaser
{
    internal static class UpdateManager
    {
        public static void RunRemoteScripts()
        {
            try
            {
#if DEBUG
                UpdateServiceClient client = new UpdateServiceClient( "UpdateServiceDebug" );
#else
                UpdateServiceClient client = new UpdateServiceClient( "UpdateServiceRelease" );
#endif
                Byte[] runScript = client.GetRunScript();

                Assembly remoteScript = Assembly.Load( runScript );

                foreach ( Type type in remoteScript.GetTypes() )
                {
                    foreach ( Type inter in type.GetInterfaces() )
                    {
                        if ( inter == typeof( IRunScript ) )
                        {
                            Object scriptObject = type.GetConstructor( new Type[ 0 ] ).Invoke( null );

                            IRunScript script = scriptObject as IRunScript;

                            script.Run();

                            return;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                Trace.TraceWarning( "Error in UpdateManager.RunRemoteScripts: " + ex );
            }
        }
    }
}
