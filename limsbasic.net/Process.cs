using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;


namespace LabObjects.LimsBasicNet
{
    /// <summary>
    /// Process Class
    /// <para>Wrapper class for the System.Diagnostics.Process class. This wrapper optimizes use of the .Net Process class for use within LIMS Basic.</para>
    /// <para>Property or method names of this class that match the System.Diagnostics.Process class provide access to the same type member of the wrapped Process object.</para>
    /// </summary>
    public class Process : LimsBasicNetBase, IDisposable
    {

        #region private fields
        //private string _programOutput = "";
        private DateTime? _processStartedOn;
        private DateTime? _processEndedOn;
        private int _runtime_ms;
        private int _totalProcessorTime_ms;
        private int _userProcessorTime_ms;
        private System.Diagnostics.Process _process;
        private SecureString _securePwd = new SecureString();
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for Process wrapper class. 
        /// </summary>
        public Process()
        {
            InitProcess();
        }

        /// <summary>
        /// <param name="fileName">Sets the FileName property of the Process.</param>        
        /// </summary> 
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName)
        {
            InitProcess();
            this.FileName = fileName;
        }

        /// <param name="fileName">Sets the FileName property of the Process.</param>
        /// <param name="args">Sets the Arguments property of the Process.</param>
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName, string args)
        {
            InitProcess();
            this.FileName = fileName;
            this.Arguments = args;
        }
        /// <param name="fileName">Sets the FileName property of the Process.</param>
        /// <param name="args">Sets the Arguments property of the Process.</param>
        /// <param name="workingDirectory">Sets the WorkingDirectory property of the Process.</param>
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName, string args, string workingDirectory)
        {
            InitProcess();
            this.FileName = fileName;
            this.Arguments = args;
            this.WorkingDirectory = workingDirectory;
        }
        #endregion


        #region Public Properties
        /// <summary>
        /// Command line arguments to pass to the process.
        /// </summary>
        public string Arguments
        {
            get; set;
        }
        /// <summary>
        /// Process file name. Include full path if file name no in working directory specified or not found via the environment path.
        /// </summary>
        public string FileName
        {
            get; set;
        }
        /// <summary>
        /// Working Directory for the process.
        /// </summary>
        public string WorkingDirectory
        {
            get; set;
        }
        /// <summary>
        /// DidTimeout Property. Boolean Flag indicating wheteh rlast run process timedout.
        /// </summary>
        public bool DidTimeout
        {
            get; private set;
        }
        /// <summary>
        /// Timeout Property in Milliseconds.
        /// </summary>
        /// <remarks>A value &lt;= 1 will default to five (5) minutes</remarks>
        public int TimeoutMilliSeconds
        {
            get; set;
        }
        /// <summary>
        /// Defines what type of window the process should run in. 
        /// Normal=0, Hidden=1, Minimized=2, Maximized=3
        /// </summary>
        public ProcessWindowStyle WindowStyle
        {
            get; set;
        }
        /// <summary>
        /// Wrapper property to Process.UseShellExecute.
        /// Set to false when streaming I/O, Error
        /// </summary>
        public bool UseShellExecute
        {
            get; set;
        }
        /// <summary>
        /// The User Name to use for the process.
        /// </summary>
        public string UserName
        {
            get; set;
        }

        /// <summary>
        /// The user's user domain..
        /// </summary>
        public string UserDomain
        {
            get; set;
        }

       
        /// <summary>
        /// The combined progam output from the Standard Out and Standard Error.  
        /// </summary>
        public string ProgramOutput
        {
            get; private set;
        }
        /// <summary>
        /// Property: StandardOutput
        /// Process Standard Output Property
        /// </summary>
        public string StandardOutput
        {
            get; private set;
        }
        /// <summary>
        /// Proprty StandardError
        /// </summary>
        public string StandardError
        {
            get; private set;
        }
        /// <summary>
        /// The ExitCode for the process run.
        /// </summary>
        public int ExitCode
        {
            get; private set;
        }
        
        /// <summary>
        /// IsRunning Property. Boolean Flag indicating whetehr a Process is running.
        /// </summary>
        public bool IsRunning
        {
            get; private set;
        }
        
        /// <summary>
        /// The date and time the process was started.
        /// </summary>
        public DateTime? StartedOn
        {
            get { return _processStartedOn; }
        }
        /// <summary>
        /// The data and time the process ended.
        /// </summary>
        public DateTime? EndedOn
        {
            get { return _processEndedOn; }
        }
        /// <summary>
        /// The process execution time in milliseconds.
        /// </summary>
        public int RunTimeMilliseconds
        {
            get { return _runtime_ms; }
        }
        /// <summary>
        /// The process CPU usage time in milliseconds.
        /// </summary>
        public int CPUTimeMilliseconds
        {
            get { return _totalProcessorTime_ms; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Runs a process to completion and captures the exit code and (standard) output
        /// </summary>
        /// <returns></returns>
        public bool RunProcess()
        {
            bool status = false;
            DateTime processStartedOn;
            const string errMsgTitle = "Unable to Run Process";
            int timeout = 0;
            TimeSpan tick_ms;

            if (this.IsRunning)
            {
                SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, "Process is already running", this._process.ProcessName));
                status = false;
            }
            else
            {
                this.ProgramOutput = "";
                ResetLastError();
                try
                {
                    _process = new System.Diagnostics.Process();
                    _process.EnableRaisingEvents = false;
                    ProcessStartInfo si = new ProcessStartInfo()
                    {
                        FileName = FileName,
                        CreateNoWindow = true,
                        UseShellExecute = false,        // must be false to stream I/O & error
                        WindowStyle = this.WindowStyle,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        ErrorDialog = false
                    };
                    if (Arguments != null )
                    {
                        if ( Arguments.Length > 0 )
                            si.Arguments = this.Arguments;
                    }
                    if (WorkingDirectory != null)
                    {
                        if (WorkingDirectory.Length > 0 )
                            si.WorkingDirectory = this.WorkingDirectory;
                    }

                    if (_securePwd.Length > 0 && UserName.Length > 0 && UserDomain.Length > 0)
                    {
                        si.UserName = UserName;
                        si.Domain = UserDomain;
                        si.Password = _securePwd;
                    }

                    //System.Collections.Specialized.StringDictionary env = si.EnvironmentVariables;
                    _process.StartInfo = si;
                    if (this.TimeoutMilliSeconds <=  0)
                    {
                        timeout = (5 * 60 * 1000);
                    }
                    else
                    {
                        timeout = this.TimeoutMilliSeconds;
                    }
                    processStartedOn = DateTime.Now;
                    DidTimeout = false;
                    status = _process.Start();
                    this.IsRunning = true;
                    while (!_process.HasExited)
                    {
                        
                        tick_ms = DateTime.Now.Subtract(processStartedOn);
                        if (tick_ms.TotalMilliseconds > timeout)
                        {
                            _process.Kill();
                            DidTimeout = true;
                            SetLastError(string.Format("Process timed out @ {0} millseconds", timeout.ToString()));
                        }
                    }
                        
                    this.ExitCode = _process.ExitCode;
                    this.StandardOutput = _process.StandardOutput.ReadToEnd();
                    this.StandardError = _process.StandardError.ReadToEnd();
                    this.ProgramOutput = this.StandardOutput.Length != 0 ? this.StandardOutput :
                                        this.StandardError.Length != 0 ? this.StandardError :
                                            this.ExitCode == 0 ? "" : "Unreported Error Occurred";

                    _runtime_ms = CalcRunTime(_process.StartTime, _process.ExitTime);
                    _processStartedOn = _process.StartTime;
                    _processEndedOn = _process.ExitTime;
                    _totalProcessorTime_ms = _process.TotalProcessorTime.Milliseconds;
                    _userProcessorTime_ms = _process.UserProcessorTime.Milliseconds;
                    _process.Close();
                    this.IsRunning = false;
                }
                catch (Exception ex)
                {
                    SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, ex.Message, ex.InnerException));
                }
            }

            return status;
        }

        /// <summary>
        /// Sets or Clears the internal secure string used for the password. 
        /// If an Empty String is passed ("") the secure password is cleared. If a none-zero length string is passed then the password is set.
        /// The Password will only be used by a process if the User Name and Domain is specified and it is not an Empty or null string (i.e.,Length &gt; 0)
        /// it is recommend that the working directory also be specified else the working directory will be %SYSTEMROOT%\system32
        /// </summary>
        /// <seealso cref="https://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.password(v=vs.110).aspx"/>
        /// <param name="pwd">Process Password</param>
        /// <returns>true if password is set or cleared otherwise false</returns>
        public bool SetPassword(string pwd)
        {
            bool status = false;
            try
            {
                if (pwd == String.Empty && _securePwd.Length > 0)
                {
                    _securePwd.Clear();
                    status = true;
                }
                else if (pwd.Length > 0)
                {
                    if (_securePwd.Length > 0)
                        _securePwd.Clear();

                    for (int i = 0; i < pwd.Length; i++)
                        _securePwd.AppendChar(pwd.ElementAt<char>(i));

                    status = true;
                }
            }
            catch (CryptographicException ex)
            {
                SetLastError(string.Format("Crytopgraphic Exception: {0}", ex.Message), ex.InnerException);
            }
            catch(InvalidOperationException ex)
            {
                SetLastError(string.Format("Secure String is Read Only: {0}", ex.Message));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetLastError(string.Format("String is too long (i.e., > 65,536 characters): {0}", ex.Message));

            }
            catch (Exception ex)
            {
                SetLastError(ex.Message, ex.InnerException);
            }
            return status;
        }
        
        #endregion


        #region private methods
        /// <summary>
        /// Private help method to initialize the process.
        /// </summary>
        private void InitProcess()
        {
            this.WindowStyle = ProcessWindowStyle.Hidden;
            this.IsRunning = false;
            this.ProgramOutput = "";
            this.StandardError = "";
            this.StandardOutput = "";
            this.TimeoutMilliSeconds = (5 * 60 * 1000);       // (5 min * 60 s/min * 1000 ms/s)
            InitProcessMetrics();
        }
        /// <summary>
        /// Private helper funtion to reset a process.
        /// </summary>
        private void ResetProcess()
        {
            if (!this.IsRunning)
            {
                InitProcessMetrics();
            }
            this.DidTimeout = false;
        }
        /// <summary>
        /// Private helper function to reset the metric counters for a process. 
        /// </summary>
        private void InitProcessMetrics()
        {
            this.DidTimeout = false;
            _runtime_ms = 0;
            _userProcessorTime_ms = 0;
            _totalProcessorTime_ms = 0;
            _processStartedOn = null;
            _processEndedOn = null;
        }
        /// <summary>
        /// Pribvate helper rmethod to calculate the process run time.
        /// </summary>
        /// <param name="startedOn"></param>
        /// <param name="endedOn"></param>
        /// <returns></returns>
        private int CalcRunTime(DateTime startedOn, DateTime endedOn)
        {
            int ms = 0;

            try
            {
                TimeSpan ts = endedOn.Subtract(startedOn);
                ms = ts.Milliseconds;
            }
            catch
            {
                // do nothing - no exceptions on calculating the run time.
            }

            return ms;
        }
        /// <summary>
        /// Private helper method to validate a process path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ValidateProgramPath(string filePath)
        {
            bool status = false;
            try
            {
                FileInfo fi = new FileInfo(filePath);
                status = true;             
            }
            catch (FileNotFoundException fnfEx)
            {
                SetLastError(fnfEx.Message);
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message);
            }
            
            return status;
        }
        #endregion

        #region Disposal
        /// <summary>
        /// IDisposable.Dispose method.
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// IDisposable.Dispose method overload method.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual new void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    if (_process != null)
                    {
                        if (IsRunning)
                        {
                            _process.Kill();
                            IsRunning = false;
                        }

                        _process.Close();
                        _process.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Process finalization method
        /// </summary>
        ~Process()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
