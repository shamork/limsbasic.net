using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
#if NET_VER_45
using System.Threading.Tasks;
#endif
using System.Security;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;
using System.Timers;




namespace LabObjects.LimsBasicNet
{
    /// <summary>
    /// Process Class
    /// <para>Wrapper class for the System.Diagnostics.Process class. This wrapper optimizes use of the .Net Process class for use within scripted languages.</para>
    /// <para>Property or method names of this class that match the System.Diagnostics.Process class provide access to the same type member of the wrapped Process object unless as noted.</para>
    /// </summary>
    public class Process : LimsBasicNetBase, IDisposable
    {
        #region private fields
        private System.Diagnostics.Process _process;
        private int _defaultTimeout_ms = 5 * 60 * 1000;     // 5 minutes
        private Timer _clock = new Timer((5 * 60 * 1000));    

        private bool _isStarted = false;
        private bool _isRunning = false;
        private bool _isOutputStreamAsync = false;            // indicates if reading output stream is sync or async
        private bool _isErrorStreamAsync = false;             // indicates if reading error (output) stream is sync or async
        private bool _isDynamicTimeout = false;

        private StreamWriter _processInputWriter;
        ProcessBuffer _bufferOutput = new ProcessBuffer();
        ProcessBuffer _bufferError = new ProcessBuffer();
        private SecureString _securePwd = new SecureString();

        private DateTime? _processStartedOn;
        private DateTime? _processEndedOn;
        private IntPtr _processorAffinity=new IntPtr(0);
        private int _runtime_ms;
        private int _totalProcessorTime_ms;
        private int _userProcessorTime_ms;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for Process wrapper class. 
        /// </summary>
        public Process()
        {
            PropertiesInit();
            ProcessClockInit();
        }

        /// <summary>
        /// Constructor for Process class that also sets the process file name.
        /// </summary> 
        /// <param name="fileName">Sets the FileName property of the Process.</param>
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName)
        {
            PropertiesInit();
            ProcessClockInit();
            this.FileName = fileName;
        }

        /// <param name="fileName">Sets the FileName property of the Process.</param>
        /// <param name="args">Sets the Arguments property of the Process.</param>
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName, string args)
        {
            PropertiesInit();
            ProcessClockInit();
            this.FileName = fileName;
            this.Arguments = args;
        }
        /// <param name="fileName">Sets the FileName property of the Process.</param>
        /// <param name="args">Sets the Arguments property of the Process.</param>
        /// <param name="workingDirectory">Sets the WorkingDirectory property of the Process.</param>
        /// <remarks>See Process Class - StartInfo property documentation on MSDN: https://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx </remarks>
        public Process(string fileName, string args, string workingDirectory)
        {
            PropertiesInit();
            ProcessClockInit();
            this.FileName = fileName;
            this.Arguments = args;
            this.WorkingDirectory = workingDirectory;
        }
        #endregion

        #region Public Properties

        #region StartInfo Properties 
        /// <summary>
        /// Command line arguments to pass to the process.
        /// </summary>
        /// <loType>string</loType>
        public string Arguments
        {
            get; set;
        }
        /// <summary>
        /// Process file name. Include full path if file name no in working directory specified or not found via the environment path.
        /// </summary>
        /// <Type>string</Type> 
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
        /// CreateNoWindow Property (see Process documentation)
        /// </summary>
        public bool CreateNoWindow
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
        /// The user's user domain.
        /// </summary>
        public string UserDomain
        {
            get; set;
        }
        /// <summary>
        /// Load User Profile when running the Process. 
        /// Note: This property is referenced if the process is being started by using the user name, password, and domain.
        /// If the value is true, the user's profile in the HKEY_USERS registry key is loaded. Loading the profile can be time-consuming. Therefore, it is best to use this value only if you must access the information in the HKEY_CURRENT_USER registry key.
        /// </summary>
        public bool LoadUserProfile
        {
            get; set;
        }
        /// <summary>
        /// Property: RedirectStandardOutput
        /// Process Flag controlling whether to redirect Standard Output Property
        /// </summary>
        public bool RedirectStandardOutput
        {
            get; set;
        }
        /// <summary>
        /// Property: RedirectStandardError
        /// Process Flag controlling whether to redirect Standard error Property
        /// </summary>
        public bool RedirectStandardError
        {
            get; set;
        }
        /// <summary>
        /// Property: RedirectStandardInput
        /// Process Flag controlling whether to redirect Standard Input Property
        /// </summary>
        public bool RedirectStandardInput
        {
            get; set;
        }
        /// <summary>
        /// Show Winodws Error Doialog if process error generated
        /// </summary>
        public bool ErrorDialog
        {
            get; set;
        }
        /// <summary>
        /// set standard output encoding
        /// </summary>
        public Encoding StandardOutputEncoding
        {
            get; set;
        }
        /// <summary>
        /// set standard error encoding
        /// </summary>
        public Encoding StandardErrorEncoding
        {
            get; set;
        }
        public string Verb
        {
            get; set;
        }
        public string[] Verbs
        {
            get
            {
                try
                {
                    ProcessStartInfo si = new ProcessStartInfo(this.FileName);
                    return si.Verbs;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
        #endregion

        public int? Id
        {
            get; private set;
        }
        public int? Handle
        {
            get; private set;
        }

        public ProcessPriorityClass PriorityClass
        {
            get;set;
        }

        /// <summary>ProcessorAffinity
        /// <paraa>Gets or sets the processors on which the threads in this process can be scheduled to run when the process is not running. 
        /// When the process is running use the method SetProcessorAffinity().</paraa>
        /// </summary>
        public int ProcessorAffinity
        {
            get { return _processorAffinity.ToInt32(); }
            set
            {
                if (!IsRunning)
                    _processorAffinity = (IntPtr)value;
                else
                    throw new Exception(string.Format("ProcessorAffinity:Process is Running - Use SetProcessorAffinity to change for a running process"));
            }
        }

        #region timeout properties
        /// <summary>
        /// DidTimeout Property. Boolean Flag indicating wheteh rlast run process timedout.
        /// </summary>
        public bool DidTimeout
        {
            get; private set;
        }
        /// <summary>
        /// DynamicTimeout - when TRUE allows Process timeout determinations to be made on based on last Output or Input Activity
        /// otherwise process timeout is based on when the process is started. Must be set before the process starts.
        /// </summary>
        public bool DynamicTimeout
        {
            get { return _isDynamicTimeout; }
            set
            {
                if (IsRunning)
                {
                    throw new Exception("Process.DynamicTimeout: Can't set property when Process is running");
                }
                else
                    _isDynamicTimeout = value;
            }
        }
        /// <summary>
        /// Timeout Property in Milliseconds.
        /// </summary>
        /// <remarks>A value &lt;= 1 will default to five (5) minutes</remarks>
        public int TimeoutMilliSeconds
        {
            get; set;
        }
        #endregion

        #region I/O Properties 
        /// <summary>
        /// Is (new) Outout Stream Data Available?
        /// </summary>
        public bool IsOutputAvailable
        {
             get { return _bufferOutput.HasUnreadData; }
        }
        /// <summary>
        /// Is (new) Error Stream Data Available?
        /// </summary>
        public bool IsErrorAvailable
        {
            get { return _bufferError.HasUnreadData; }
        }

        ///// <summary>
        ///// The combined progam output from the Standard Out and Standard Error.  
        ///// </summary>
        //public string ProgramOutput
        //{
        //    get; private set;
        //}
        public string Output
        {
            get { return _bufferOutput.Data; }
        }
        public string ErrorOutput
        {
            get { return _bufferError.Data; }
        }
  
        #endregion

        #region Exit Code and Running Status
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
            get ; private set;
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsStarted
        {
            get { return _isStarted; }
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
        #endregion

        #region process resource metrics

        //Public property NonpagedSystemMemorySize    Obsolete.Gets the nonpaged system memory size allocated to this process.
        //Public property NonpagedSystemMemorySize64 Gets the amount of nonpaged system memory allocated for the associated process.
        /// <summary>
        /// NonpagedSystemMemorySize64 - Gets the amount of nonpaged system memory allocated for the associated process.
        /// </summary>
        public Int64 NonpagedSystemMemorySize64
        {
            get; private set;
        }

        //Public property PagedMemorySize Obsolete. Gets the paged memory size.
        //Public property PagedMemorySize64 Gets the amount of paged memory allocated for the associated process.
        /// <summary>
        /// PagedMemorySize64 - Gets the amount of nonpaged system memory allocated for the associated process.
        /// </summary>
        public Int64 PagedMemorySize64
        {
            get; private set;
        }

        //Public property PagedSystemMemorySize Obsolete. Gets the paged system memory size.
        //Public property PagedSystemMemorySize64 Gets the amount of pageable system memory allocated for the associated process.
        /// <summary>
        /// PagedSystemMemorySize64 - Gets the amount of pageable system memory allocated for the associated process.
        /// </summary>
        public Int64 PagedSystemMemorySize64
        {
            get; private set;
        }
        //Public property PeakPagedMemorySize Obsolete. Gets the peak paged memory size.
        //Public property PeakPagedMemorySize64 Gets the maximum amount of memory in the virtual memory paging file used by the associated process.
        /// <summary>
        /// PeakPagedMemorySize64 - Gets the maximum amount of memory in the virtual memory paging file used by the associated process.
        /// </summary>
        public Int64 PeakPagedMemorySize64
        {
            get; private set;
        }
        //Public property PeakVirtualMemorySize   Obsolete.Gets the peak virtual memory size.
        //Public property PeakVirtualMemorySize64 Gets the maximum amount of virtual memory used by the associated process.
        /// <summary>
        /// PeakVirtualMemorySize64 - Gets the maximum amount of virtual memory used by the associated process.
        /// </summary>
        public Int64 PeakVirtualMemorySize64
        {
            get; private set;
        }
        //Public property PeakWorkingSet  Obsolete.Gets the peak working set size for the associated process.
        //Public property PeakWorkingSet64 Gets the maximum amount of physical memory used by the associated process.
        /// <summary>
        /// PeakWorkingSet64 - Gets the maximum amount of physical memory used by the associated process.
        /// </summary>
        public Int64 PeakWorkingSet64
        {
            get; private set;
        }

        //Public property PrivateMemorySize Obsolete. Gets the private memory size.
        //Public property PrivateMemorySize64 Gets the amount of private memory allocated for the associated process.
        /// <summary>
        /// PrivateMemorySize64 - Gets the amount of private memory allocated for the associated process.
        /// </summary>
        public Int64 PrivateMemorySize64
        {
            get; private set;
        }

        // Public property MaxWorkingSet Gets or sets the maximum allowable working set size for the associated process.
        //Public property MinWorkingSet Gets or sets the minimum allowable working set size for the associated process.
        //Public property Modules Gets the modules that have been loaded by the associated process.
        //Public property PriorityBoostEnabled Gets or sets a value indicating whether the associated process priority should temporarily be boosted by the operating system when the main window has the focus.
        //Public property PriorityClass Gets or sets the overall priority category for the associated process.
        //Public property PrivilegedProcessorTime Gets the privileged processor time for this process.
        //Public property ProcessName Gets the name of the process.
        //Public property ProcessorAffinity Gets or sets the processors on which the threads in this process can be scheduled to run.
        //Public property Responding Supported by the.NET Compact Framework Responding  Gets a value indicating whether the user interface of the process is responding.
        //Public property SessionId Gets the Terminal Services session identifier for the associated process.
        #endregion

        #region process runtime metrics
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

        #endregion

        #region Public Methods

        /// <summary>
        /// CloseMainWindow Method - Can called when started with Start methods.
        /// If process has a Windows message loop it should respond by closing the window.
        /// </summary>
        /// <returns></returns>
        public bool CloseMainWindow()
        {
            bool status = false;
            const string errMsg = "Unable to CloseMainWindow";
            try
            {
                status = _process.CloseMainWindow();
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("{0}: {1}", errMsg, ex.Message), ex);
            }
            return status;
        }
        
        public bool Close()
        {
            bool status = false;
            try
            {                
                _process.Close();
                status = true;
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Process.Close: Failed: {0}", ex.Message), ex);
            }
            return status;
        }

        public string ErrorOutputRead()
        {
            return _bufferError.Read();
        }

        /// <summary>
        /// Write to the process Input Stream
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public bool InputWrite(string inputData)
        {
            bool status = false;
            try
            {
                if (this.IsRunning && this.RedirectStandardInput)
                {
                    //_process.StandardInput.Write(inputData);
                    _processInputWriter.Write(inputData);
                    if (_isDynamicTimeout)
                        ProcessClockRestart();
                    status = true;
                }
                else if (!this.RedirectStandardInput)
                {
                    throw new InvalidOperationException("Process Input is not redirected!");
                }
                else if (!this.IsRunning)
                {
                    throw new InvalidOperationException("Process is not running!");
                }
            }
            catch (InvalidOperationException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message), ex.InnerException);
            }
            return status;
        }
        /// <summary>
        /// InputWriteLine
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public bool InputWriteLine(string inputData)
        {
            bool status = false;
            try
            {
                if (this.IsRunning && this.RedirectStandardInput)
                {
                    //_process.StandardInput.Write(inputData);
                    _processInputWriter.WriteLine(inputData);
                    if (_isDynamicTimeout)
                        ProcessClockRestart();
                    status = true;
                }
                else if (!this.RedirectStandardInput)
                {
                    throw new InvalidOperationException("Process Input is not redirected!");
                }
                else if (!this.IsRunning)
                {
                    throw new InvalidOperationException("Process is not running!");
                }
            }
            catch (InvalidOperationException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message), ex.InnerException);
            }
            return status;
        }

        /// <summary>
        /// Kill Process Method
        /// </summary>
        /// <returns></returns>
        public bool Kill()
        {
            bool status = false;
            if (IsRunning)
            {
                try
                {
                    ProcessKill();
                    status = true;
                }
                catch (Exception ex)
                {
                    SetLastError(string.Format("Unable to Kill Process: {0}", ex.Message), ex);
                }
            }
            else
            {
                SetLastError(string.Format("Unable to Kill Process: {0}", "Process Not Running"));
            }
            return status;
        }

        /// <summary>
        /// Reads from the asynchronous output buffer any data that has not been Read when IsOutputAvailable is true. This is used in conjunction with the Start Method
        /// and allows for retrieving process output while the process is executing. 
        /// </summary>
        /// <returns>String - all the data in the output buffer that has not been read</returns>
        public string OutputRead()
        {
            return _bufferOutput.Read();
        }

        /// <summary>SetErrorOutputEncoding
        /// Sets the process Standard Error Output encoding based on a encoding name. 
        /// Currently supported (case insensitive) values: ASCII, Unicode, UTF8, UTF32, UTF7, BigEndianUnicode
        /// The process can not be running.
        /// </summary>
        /// <param name="encodingName">Case insensitive string defining one of the known Encoding values: ASCII, Unicode, UTF8, UTF32, UTF7, BigEndianUnicode</param>
        /// <returns>True if the encoding was set to the encodingName, otherwise the value is false and LastError and LastErrorDetail contain the details of the failure.</returns>
        public bool SetErrorOutputEncoding(string encodingName)
        {
            Encoding enc = null;
            bool status = false;
            if (!IsRunning)
            {
                enc = GetEncodingFromName(encodingName);
                if (enc != null)
                {
                    try
                    {
                        //_process.StartInfo.StandardErrorEncoding = enc;
                        this.StandardErrorEncoding = enc;
                        status = true;
                    }
                    catch (Exception ex)
                    {
                        SetLastError(string.Format("SetErrorOutputEncoding: {0}", ex.Message), ex);
                    }
                }
                else
                    SetLastError(string.Format("SetErrorOutputEncoding:{0}", this.LastError));
            }
            else
                SetLastError(string.Format("SetErrorOutputEncoding: {0}", "Process is Running"));

            return status;
        }

        /// <summary>SetOutputEncoding
        /// Sets the process Standard Output encoding based on a encoding name. 
        /// Currently supported (case insensitive) values: ASCII, Unicode, UTF8, UTF32, UTF7, BigEndianUnicode
        /// The process can not be running
        /// </summary>
        /// <param name="encodingName">Case insensitive string defining one of the known Encoding values: ASCII, Unicode, UTF8, UTF32, UTF7, BigEndianUnicode</param>
        /// <returns>True if the encoding was set to the encodingName, otherwise the value is false and LastError and LastErrorDetail contain the details of the failure.</returns>
        public bool SetOutputEncoding(string encodingName)
        {
            Encoding enc = null;
            bool status = false;
            if (!IsRunning)
            {
                enc = GetEncodingFromName(encodingName);
                if (enc != null)
                {
                    try
                    {
                        //_process.StartInfo.StandardOutputEncoding = enc;
                        this.StandardOutputEncoding = enc;
                        status = true;
                    }
                    catch (Exception ex)
                    {
                        SetLastError(string.Format("SetOutputEncoding: {0}", ex.Message), ex);
                    }
                }
                else
                    SetLastError(string.Format("SetOutputEncoding:{0}", this.LastError));
            }
            else
                SetLastError(string.Format("SetOutputEncoding: {0}", "Process is not Running"));

            return status;
        }

        /// <summary>SetPriorityClass
        /// <para>Sets the process ProrityClass based on a string (priorityClassName) as long as the (case insensitive) string matches a value in the
        /// enumeration ProcessPriorityClass:
        /// Normal, Idle, High, RealTime, AboveNormal, BelowNormal
        /// </para>
        /// </summary>
        /// <param name="priorityClassName"></param>
        /// <returns></returns>
        public bool SetPriorityClass(string priorityClassName)
        {
            bool status = false;
            if (!IsRunning)
            {
                try
                { 
                    switch (priorityClassName.ToLower())
                    {
                        case "normal":
                            this.PriorityClass = ProcessPriorityClass.Normal;
                            status = true;
                            break;
                        case "idle":
                            this.PriorityClass = ProcessPriorityClass.Idle;
                            status = true;
                            break;
                        case "high":
                            this.PriorityClass = ProcessPriorityClass.High;
                            status = true;
                            break;
                        case "realtime":
                            this.PriorityClass = ProcessPriorityClass.RealTime;
                            status = true;
                            break;
                        case "abovenormal":
                            this.PriorityClass = ProcessPriorityClass.AboveNormal;
                            status = true;
                            break;
                        case "belownormal":
                            this.PriorityClass = ProcessPriorityClass.BelowNormal;
                            status = true;
                            break;
                        default:
                            throw new Exception(string.Format("SetPriorityClass: Name does not match ProcessPriorityClass Enumeration: {0}", priorityClassName ));
                    }
                }
                catch (Exception ex)
                {
                    SetLastError(string.Format("SetPriorityClass: {0}", ex.Message), ex);
                }
            }
            else
                SetLastError(string.Format("SetPriorityClass: {0}", "Process is Running"));

            return status;
        }

        /// <summary>SetProcessorAffinity
        /// <para>Sets the processor affinity for a runnng/non-running process; this method must be used when the process is running.</para>
        /// </summary>
        /// <param name="processorAffinity"></param>
        /// <returns>True if successfully updated processor affinity otherwise false and LastError should be checked.</returns>
        public bool SetProcessorAffinity(int processorAffinity)
        {
            bool status=false;
            try
            {
                if (IsRunning)
                {
                    _process.ProcessorAffinity = (IntPtr) processorAffinity;
                    status = true;
                    this.ProcessorAffinity = processorAffinity;
                }
                else
                    this.ProcessorAffinity = processorAffinity;
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("SetProcessorAffinity: {0}", ex.Message), ex);
            }
            return status;
        }

        /// <summary>
        /// Start Process
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            bool status = false;
            try
            {
                status = ProcessStart();
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Process.Start: Failed: {0}", ex.Message), ex);
            }
            return status;
        }

        public bool Start(bool outputAsync)
        {
            bool status = false;
            try
            {
                _isOutputStreamAsync = outputAsync;
                status = ProcessStart();
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Process.Start: Failed: {0}", ex.Message), ex);
            }
            return status;
        }

        /// <summary>
        /// WaitForExit
        /// </summary>
        /// <returns></returns>
        public bool WaitForExit()
        {
            bool status = false;
            try
            {
                ProcessWaitForExit();
                ProcessTeardown(_process);
                status = true;
            }
            catch(Exception ex)
            {
                SetLastError(string.Format("Process.WaitForExit: Failed: {0}", ex.Message), ex);
            }
            return status;
        }

        /// <summary>
        /// Runs a hidden process to completion and captures the exit code and (standard) output
        /// </summary>
        /// <returns></returns>
        public bool RunHidden()
        {
            bool status = false;
            DateTime processStartedOn;
            const string errMsgTitle = "Unable to Run Hidden Process";

            if (this.IsRunning)
            {
                SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, "Process is already running", this._process.ProcessName));
                status = false;
            }
            else
            {
                ResetLastError();
                try
                {
                    _process = new System.Diagnostics.Process();
                    _process.EnableRaisingEvents = true;
                    _process.Exited += new EventHandler(ProcessExited);
                    _process.ErrorDataReceived += new DataReceivedEventHandler(ProcessErrorDataReceived);
                    //_process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
                    ProcessStartInfo si = StartInfoNewFromParent();                    
                    si.UseShellExecute = false;
                    si.CreateNoWindow = true;
                    si.WindowStyle = ProcessWindowStyle.Hidden;
                    si.ErrorDialog = false;
                    si.RedirectStandardOutput = true;
                    si.RedirectStandardError = true;
                    si.RedirectStandardInput = false;
                    _process.StartInfo = si;
 
                    DidTimeout = false;
#if DEBUG
                    Debug.Write(string.Format("Starting Process: {0}....", si.FileName));
#endif
                    _isOutputStreamAsync = false;
                    _isErrorStreamAsync = true;

                    if (_process.Start())
                    {
                        this.IsRunning = true;
                        this._isStarted = true;
                        this._isRunning = true;
                        _process.BeginErrorReadLine();
                        processStartedOn = _process.StartTime;
                        ProcessClockStart();
                        this.Handle = _process.Handle.ToInt32();
                        this.Id = _process.Id;
                        this.PriorityClass = _process.PriorityClass;
                        _processorAffinity = _process.ProcessorAffinity;
                        ProcessRefreshProperties();

#if DEBUG
                        Debug.WriteLine(String.Format("Process Started: Handle:{0}\r\nProcess ID: {1}",
                                _process.Handle,
                                _process.Id));
#endif

                        while (_isRunning)
                            ;

                        ProcessWaitForExit();
                        ProcessTeardown(_process);
                        return true;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, ex.Message, ex.InnerException));
                }
                catch (Exception ex)
                {
                    SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, ex.Message, ex.InnerException));
                }
            }

            return status;
        }

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
                //this.Output = "";
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

                    if (LoadUserProfile)
                    {
                        
                    }

                    if (_securePwd.Length > 0 && UserName.Length > 0 && UserDomain.Length > 0)
                    {
                        si.UserName = UserName;
                        si.Domain = UserDomain;
                        si.Password = _securePwd;
                    }


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
                    Debug.Write(string.Format("Starting Process: {0}....", _process.ProcessName));
                    status = _process.Start();
                    Debug.WriteLine(String.Format("Status: {0}", status.ToString()));
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
                    //this.Output = _process.StandardOutput.ReadToEnd();
                    //this.ErrorOutput = _process.StandardError.ReadToEnd();
                    //this.ProgramOutput = this.StandardOutput.Length != 0 ? this.StandardOutput :
                    //                    this.StandardError.Length != 0 ? this.StandardError :
                    //                        this.ExitCode == 0 ? "" : "Unreported Error Occurred";

                    _runtime_ms = CalcRunTime(_process.StartTime, _process.ExitTime);
                    _processStartedOn = _process.StartTime;
                    _processEndedOn = _process.ExitTime;
                    _totalProcessorTime_ms = _process.TotalProcessorTime.Milliseconds;
                    _userProcessorTime_ms = _process.UserProcessorTime.Milliseconds;
                    _process.Close();
                    this.IsRunning = false;
                }
                catch (InvalidOperationException ex)
                {
                    SetLastError(string.Format("{0}: {1}: {2}", errMsgTitle, ex.Message, ex.InnerException));
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
        /// <remarks>https://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.password(v=vs.110).aspx</remarks>
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
        /// <summary>
        /// Write to the process Input Stream
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public bool WriteProcessInput(string inputData)
        {
            bool status = false;
            try
            {
                if (this.IsRunning && this.RedirectStandardInput)
                {
                    //_process.StandardInput.Write(inputData);
                    _processInputWriter.Write(inputData);
                    if (_isDynamicTimeout)
                        ProcessClockRestart();
                    status = true;
                }
                else if (!this.RedirectStandardInput)
                {
                    throw new InvalidOperationException("Process Input is not redirected!");
                }
                else if (!this.IsRunning)
                {
                    throw new InvalidOperationException("Process is not running!");
                }
            }
            catch (InvalidOperationException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Unable to Write Process Input: {0}", ex.Message), ex.InnerException);
            }
            return status;
        }

        static System.Diagnostics.Process GetHostProcess()
        {
            return System.Diagnostics.Process.GetCurrentProcess();
        }

        #endregion

        #region private methods
        /// <summary>GetEncodingFromName
        /// <para></para>Gets an Encoding based on a Name defined by the string encodingName which can be one of the predefined encodings
        /// in the System.Text.Encoding class. When GetEncodingFromName makes the determination it uses a case sensitive comparision of the 
        /// name to one of the following values: ASCII, Unicode, UTF8, UTF32, UTF7, BigEndianUnicode
        /// </summary>
        /// <param name="encodingName">String defining a Encoding name to retrieve</param>
        /// <returns>Encoding or null depending on whether the encodingName matched one of the encodings supported. </returns>
        private Encoding GetEncodingFromName(string encodingName)
        {
            Encoding enc = null;
            switch (encodingName.ToLower())
            {
                case "ascii":
                    enc = Encoding.ASCII;
                    break;
                case "unicode":
                    enc = Encoding.Unicode;
                    break;
                case "utf7":
                    enc = Encoding.UTF7;
                    break;
                case "utf8":
                    enc = Encoding.UTF8;
                    break;
                case "utf32":
                    enc = Encoding.UTF32;
                    break;
                case "bigendianunicode":
                    enc = Encoding.BigEndianUnicode;
                    break;
                default:
                    enc = null;
                    throw new Exception(string.Format("SetErrorOutputEncoding: Encoding not supported: {0}", encodingName));
            }
            return enc;
        }
        #region Clock Methods
        private void ProcessClockRestart()
        {
            ProcessClockStop();
            ProcessClockStart();
        }
        private void ProcessClockStart()
        {
            this.DidTimeout = false;
            _clock.Enabled = false;
            // set interval to time out value
            _clock.Interval = TimeoutIntervalGet();
            _clock.Enabled = true;
            _clock.Start();
        }
        private void ProcessClockStop()
        {
            _clock.Stop();
            _clock.Enabled = false;
            //_clock.Interval = 10000;
        }

        private void ProcessClockInit()
        {
            this.DidTimeout = false;
            _clock.Elapsed += new ElapsedEventHandler(ProcessClockElapsed);
        }

        /// <summary>
        /// Returns the Timeout Interval in milliseconds. 
        /// If the TimeoutMilliseconds property is less than or equal to zero the function returns the default timeout interval (e.g., 300000 ms or 5 minutes)
        /// otherwise the function returns the TimeoutMilliseconds property value
        /// </summary>
        /// <returns></returns>
        private int TimeoutIntervalGet()
        {
            int timeout = 0;
            if (this.TimeoutMilliSeconds <= 0)
            {
                timeout = _defaultTimeout_ms;
            }
            else
            {
                timeout = this.TimeoutMilliSeconds;
            }
            return timeout;
        }
        #endregion

        private bool ProcessCloseMainWindow()
        {
            bool status = false;
            const string errMsg = "Unable to CloseMainWindow";
            if (IsRunning && (_process != null) && !_process.HasExited)
            {
                try
                {
                    status = _process.CloseMainWindow();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("{0}: {1}", errMsg, ex.Message), ex);
                }
            }
            else
            {
                throw new Exception(string.Format("{0}: {1}", errMsg, "Process Not Running"));
            }
            return status;
        }

        /// <summary>
        /// ProcessCreate - creates a System.Diagnostics.Process with event handlers for Exited, OutputDataReceived and ErrorDataReceived events 
        /// added and EnableRaisingEvents = true
        /// </summary>
        /// <returns></returns>
        private System.Diagnostics.Process ProcessCreate()
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.Exited += new EventHandler(ProcessExited);
                process.ErrorDataReceived += new DataReceivedEventHandler(ProcessErrorDataReceived);
                process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
                process.EnableRaisingEvents = true;
                return process;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to Create Process: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// ProcessIOPrep
        /// </summary>
        private void ProcessIOPrep(System.Diagnostics.Process p)
        {
            ProcessIOClear();
        }
        private void ProcessIOClear()
        {
            try
            {
#if NET_VER_35 && !NET_VER_40
                //_outputBuffer.Length = 0;
                //_errorBuffer.Length = 0;
#else
                //_outputBuffer.Clear();
                //_errorBuffer.Clear();
#endif
                _bufferOutput.Clear();
                _bufferError.Clear();
            }
            catch (Exception ex)
            {

            }
        }
        private bool ProcessRefresh()
        {
            bool status = false;
            if (IsRunning)
            {
                _process.Refresh();
                ProcessRefreshProperties();
                status = true;
            }
            else
            {

            }
            return status;
        }
        private void ProcessRefreshProperties()
        {
            if (IsRunning && !_process.HasExited)
            {
                this.PeakWorkingSet64 = _process.PeakWorkingSet64;
                this.NonpagedSystemMemorySize64 = _process.NonpagedSystemMemorySize64;
                this.PagedMemorySize64 = _process.PagedMemorySize64;
                this.PagedSystemMemorySize64 = _process.PagedSystemMemorySize64;
                this.PeakPagedMemorySize64 = _process.PeakPagedMemorySize64;
                this.PeakVirtualMemorySize64 = _process.PeakVirtualMemorySize64;
                this.PeakWorkingSet64 = _process.PeakWorkingSet64;
                this.PrivateMemorySize64 = _process.PrivateMemorySize64;
            }
        }
        private void ProcessReset()
        {
            if (!_isRunning)
            {
                DidTimeout = false;
                IsRunning = false;
                _isRunning = false;
                _isStarted = false;
                _processEndedOn = null;
                _processStartedOn = null;
                _clock.Enabled = false;
                Handle = null;
                Id = null;
                this.PriorityClass = ProcessPriorityClass.Normal;
                this.ProcessorAffinity = 0;
                ProcessIOClear();
                // clear metrics
            }
            else
                throw new Exception("ProcessReset:Failed:Process is Running");
        }
        private bool ProcessStart()
        {
            bool status = false;
            try
            {
                if (!IsRunning)
                {
                    ProcessReset();
                    _process = new System.Diagnostics.Process();
                    _process.EnableRaisingEvents = true;
                    _process.Exited += new EventHandler(ProcessExited);
                    ProcessStartInfo si = StartInfoNewFromParent();
                    if (this.RedirectStandardOutput && _isOutputStreamAsync)
                    {
                        _process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
                    }
                    if (this.RedirectStandardError && _isErrorStreamAsync)
                    {
                        _process.ErrorDataReceived += new DataReceivedEventHandler(ProcessErrorDataReceived);
                    }

                    _process.StartInfo = si;

                    if (_process.Start())
                    {
                        _isStarted = true;
                        _isRunning = true;
                        IsRunning = true;
                        this.Handle = _process.Handle.ToInt32();
                        this.Id = _process.Id;
                        this.PriorityClass = _process.PriorityClass;
                        _processorAffinity = _process.ProcessorAffinity;
                        ProcessRefreshProperties();

                                                //this.StartedOn = _process.StartTime;
                                                _processStartedOn = _process.StartTime;       // don't really need this
                        ProcessClockStart();
                        if (this.RedirectStandardOutput && _isOutputStreamAsync)
                        {
                            _process.BeginOutputReadLine();
                        }
                        if (this.RedirectStandardError && _isErrorStreamAsync)
                        {
                            _process.BeginErrorReadLine();
                        }
                        if (this.RedirectStandardInput)
                        {
                            _processInputWriter = _process.StandardInput;
                        }
                        IsRunning = true;
                        status = true;
                    }
                    else
                    {
                        SetLastError("Process Failed to Start");
                    }
                }
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message, ex.InnerException);
            }
            return status;

        }
        private void ProcessTeardown(System.Diagnostics.Process p)
        {
            try
            {
                if (p != null)
                {
                    p.Close();
                    p.Dispose(); 
                    // check _processOutputReader, _processErrorReader, _processInputWriter?
                }
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Private help method to initialize the process.
        /// </summary>
        private void PropertiesInit()
        {
            this.IsRunning = false;     // this should always be calculated
            _isRunning = false;
            _isStarted = false;
            _processEndedOn = null;
            _processStartedOn = null;
            this.DidTimeout = false;

            this.FileName = "";
            this.Arguments = "";
            this.Verb = "";
            this.WorkingDirectory = "";

            this.WindowStyle = ProcessWindowStyle.Normal;
            this.CreateNoWindow = false;    // default in System.Diagnostics.Process is false
            this.UseShellExecute = true;    // default in System.Diagnostics.Process is true
            this.LoadUserProfile = false;
            this.TimeoutMilliSeconds = _defaultTimeout_ms;
            this.RedirectStandardInput = false;
            this.RedirectStandardOutput = false;
            this.RedirectStandardError = false;
            this.StandardErrorEncoding = null;
            this.StandardOutputEncoding = null;
            this.PriorityClass = ProcessPriorityClass.Normal;
            this.ProcessorAffinity = 0;
            this.Handle = null;
            this.Id = null;

            ProcessIOClear();
            InitProcessMetrics();
        }
        /// <summary>
        /// Private helper funtion to reset a process.
        /// </summary>
        
        /// <summary>
        /// Private helper function to reset the metric counters for a process. 
        /// </summary>
        private void InitProcessMetrics()
        {
            _runtime_ms = 0;
            _userProcessorTime_ms = 0;
            _totalProcessorTime_ms = 0;
        }

        private void ProcessKill()
        {
            if (IsRunning)
            {
                try
                {
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("ProcessKill: Unable to Kill Process: {0}", ex.Message), ex);
                }
                finally
                {
                    ProcessWaitForExit();
                    this.ExitCode = _process.ExitCode;
                    _processEndedOn = _process.ExitTime;
                    _isRunning = false;
                    IsRunning = false;                    
                }
            }
            else
            {
                throw new Exception(string.Format("ProcessKill: Unable to Kill Process: {0}", "Process Not Running"));
            }
        }

        /// <summary>
        /// called on ProcessRefresh() and ProcessExited()
        /// </summary>
        private void ProcessMapRunProperties()
        {
            try
            {
                _processEndedOn = _process.ExitTime;
                
            }
            catch (Exception ex)
            {

            }
        }

        private void ProcessOutputReadToEnd()
        {
            if (!_isOutputStreamAsync)
            {
                _bufferOutput.Append(_process.StandardOutput.ReadToEnd());
            }
        }
        private void ProcessWaitForExit()
        {
            const string errMsg = "ProcessWaitForExit Failed";

            ResetLastError();
            try
            {
                if (_process.StartInfo.RedirectStandardOutput && !_isOutputStreamAsync)
                {
                    try
                    {
                        string s = _process.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrEmpty(s))
                            _bufferOutput.Append(s);
                    }
                    catch
                    {
                    }
                }
                _process.WaitForExit();
                this.ExitCode = _process.ExitCode;
                _processEndedOn = _process.ExitTime;
            }
            catch (Win32Exception ex)
            {
                throw new Exception(string.Format("{0}: {1}", errMsg, ex.Message), ex);
            }
            catch (SystemException ex)
            {
                throw new Exception(string.Format("{0}: {1}", errMsg, ex.Message), ex);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0}: {1}", errMsg, ex.Message), ex);
            }
        }
 

        private void ProcessWrapUp()
        {
            _runtime_ms = CalcRunTime((DateTime)_processStartedOn, (DateTime)_processEndedOn);
            _totalProcessorTime_ms = _process.TotalProcessorTime.Milliseconds;
            _userProcessorTime_ms = _process.UserProcessorTime.Milliseconds;
        }

        private ProcessStartInfo StartInfoNewFromParent()
        {
            try
            {
                ProcessStartInfo si = new ProcessStartInfo()
                {
                    FileName = this.FileName,
                    CreateNoWindow = this.CreateNoWindow,
                    UseShellExecute = this.UseShellExecute,
                    LoadUserProfile = this.LoadUserProfile,
                    WindowStyle = this.WindowStyle,
                    RedirectStandardOutput = this.RedirectStandardOutput,
                    RedirectStandardError = this.RedirectStandardError,
                    RedirectStandardInput = this.RedirectStandardInput,
                    ErrorDialog = this.ErrorDialog
                };
                if (this.StandardOutputEncoding != null)
                {
                    si.StandardOutputEncoding = this.StandardOutputEncoding;
                }
                if (this.StandardErrorEncoding != null)
                {
                    si.StandardErrorEncoding = this.StandardErrorEncoding;
                }
                if (!string.IsNullOrEmpty(this.Arguments))
                        si.Arguments = this.Arguments;
                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                        si.WorkingDirectory = this.WorkingDirectory;
                if (_securePwd.Length > 0 && UserName.Length > 0 && UserDomain.Length > 0)
                {
                    si.UserName = UserName;
                    si.Domain = UserDomain;
                    si.Password = _securePwd;
                }
                return si;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to Map Start Info From Parent: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Private helper rmethod to calculate the process run time.
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

        #region events

        /// <summary>
        /// Process Clock used to monitor process timeout has elapsed.
        /// Clock Interval is set to the timeout interval and dynamic timeouts may reset the clock based on
        /// time of last process I/O activity
        /// </summary>
        /// <param name="source">Internal Process Clock (Timer)</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void ProcessClockElapsed(object source, ElapsedEventArgs e)
        {
            if (IsRunning)
            {
                if (!_process.HasExited)
                {
                    try
                    {
                        DidTimeout = true;      // do this first to signal to wrap up process 
                        ProcessKill();
                        SetLastError(string.Format("Process timed-out"));
                    }
                    catch (Exception ex)
                    {
                        // this should be logged to a Windows Log.
                        SetLastError(string.Format("ProcessClockElapsed: Unable to Kill Process: {0}", ex.Message), ex);
                    }
                }
                else
                {
                    IsRunning = false;
                }
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(String.Format("ProcessData Received: {0}", e.Data ));
            if (!String.IsNullOrEmpty(e.Data))
            {
                _bufferOutput.AppendLine(e.Data);
                if (_isDynamicTimeout)
                    ProcessClockRestart();
            }
        }
        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            _bufferError.AppendLine(e.Data);
            if (_isDynamicTimeout)
                ProcessClockRestart();
        }

        private void ProcessExited(object sender, System.EventArgs e)
        {
            try
            {
                System.Diagnostics.Process p = (System.Diagnostics.Process) sender;
                if (!_isOutputStreamAsync && RedirectStandardOutput)
                {
                    if (!DidTimeout)
                    {
                        try
                        {
                            string s = _process.StandardOutput.ReadToEnd();
                            if (!string.IsNullOrEmpty(s))
                                _bufferOutput.Append(s);
                        }
                        catch
                        {
                        }
                    }
                }
                this.ExitCode = p.ExitCode;
                this._isRunning = false;
                this.IsRunning = false;
                _processEndedOn = p.ExitTime;
                ProcessClockStop();
                ProcessWrapUp();
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("ProcessExited: {0}", ex.Message), ex);
            }
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
