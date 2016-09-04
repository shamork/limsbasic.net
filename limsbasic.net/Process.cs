using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;


namespace limsbasic.net
{
    /// <summary>
    /// Process Class
    /// <para>Wrapper class for the System.Diagnostics.Process class. This wrapper optimizes use of the .Net Process class for use within LIMS Basic.</para>
    /// <para>Property or method names of this class that match the System.Diagnostics.Process class provide access to the same type member of the wrapped Process object.</para>
    /// </summary>
    public class Process : LimsBasicNet, IDisposable
    {

        #region private fields
        private string _programOutput = "";
        private int _exitCode;
        private DateTime? _processStartedOn;
        private DateTime? _processEndedOn;
        private int _runtime_ms;
        private int _totalProcessorTime_ms;
        private int _userProcessorTime_ms;
        private bool _isRunning = false;
        private System.Diagnostics.Process _process;
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
        /// Defines what type of window the process should run in. TODO: Check these Hidden=0, Minimized=1, Maximized=3, Normal=4
        /// </summary>
        public ProcessWindowStyle WindowStyle
        {
            get; set;
        }
        /// <summary>
        /// Wrapper property to Process.UseShellExecute.
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
        /// 
        /// </summary>
        public string UserPassword
        {
            get; set;
        }

       
        /// <summary>
        /// The combined progam output from the Standard Out and Standard Error.  
        /// </summary>
        public string ProgramOutput
        {
            get { return _programOutput; }
        }
        /// <summary>
        /// The ExitCode for the process run.
        /// </summary>
        public int ExitCode
        {
            get { return _exitCode; }
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
            const string errMsgTitle = "Unable to Run Process";

            if (_isRunning)
            {
                SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, "Process is already running", this._process.ProcessName));
                status = false;
            }
            else
            {
                _programOutput = "";
                ResetLastError();
                try
                {
                    _process = new System.Diagnostics.Process();
                    _process.EnableRaisingEvents = false;
                    ProcessStartInfo si = new ProcessStartInfo()
                    {
                        FileName = FileName,
                        Arguments = Arguments.Length > 0 ? Arguments : "",
                        WorkingDirectory = WorkingDirectory.Length > 0 ? WorkingDirectory : "",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = this.WindowStyle,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        ErrorDialog = false
                    };
                    //System.Collections.Specialized.StringDictionary env = si.EnvironmentVariables;
                    _process.StartInfo = si;
                    status = _process.Start();
                    _isRunning = true;
                    while (!_process.HasExited)
                        ;
                    _exitCode = _process.ExitCode;
                    var so = _process.StandardOutput.ReadToEnd();
                    var se = _process.StandardError.ReadToEnd();
                    _programOutput = so.Length != 0 ? so :
                                        se.Length != 0 ? se :
                                            _exitCode == 0 ? "" : "Unreported Error Occurred";

                    _runtime_ms = CalcRunTime(_process.StartTime, _process.ExitTime);
                    _processStartedOn = _process.StartTime;
                    _processEndedOn = _process.ExitTime;
                    _totalProcessorTime_ms = _process.TotalProcessorTime.Milliseconds;
                    _userProcessorTime_ms = _process.UserProcessorTime.Milliseconds;
                    _process.Close();
                    _isRunning = false;
                }
                catch (Exception ex)
                {
                    SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, ex.Message, ex.InnerException));
                }
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
            _isRunning = false;
            InitProcessMetrics();
        }
        /// <summary>
        /// Private helper funtion to reset a process.
        /// </summary>
        private void ResetProcess()
        {
            if (!_isRunning)
            {
                InitProcessMetrics();
            }
        }
        /// <summary>
        /// Private helper function to reset the metric counters for a process. 
        /// </summary>
        private void InitProcessMetrics()
        {
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
