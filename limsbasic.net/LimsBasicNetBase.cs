using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NET_VER_45
using System.Threading.Tasks;
#endif

namespace LabObjects.LimsBasicNet
{
    /// <summary>
    /// LimsBasicNetBase - base class for limsbase.net class library.
    /// </summary>
    public abstract class LimsBasicNetBase : IDisposable
    {
        #region Private fields
        private StringBuilder _lastError = new StringBuilder("No Error");
        private StringBuilder _lastErrorDetail = new StringBuilder("");
        private bool _isDisposed = false;
        #endregion



        #region Constructors
        internal LimsBasicNetBase() { }
        #endregion

        #region Public Properties
        /// <summary>
        /// Read only public property that contains the last error message trapped during library operations.
        /// </summary>
        public string LastError
        {
            get { return _lastError.ToString(); ; }
        }
        /// <summary>
        /// Read only public property that contains additonal details of the last error trapped during libray operations (if available).
        /// </summary>
        public string LastErrorDetail
        {
            get { return _lastErrorDetail.ToString(); }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Protected method to set the LastError property.
        /// </summary>
        /// <param name="errMsg">The message to set as the LastError property.</param>
        protected void SetLastError(string errMsg)
        {
        #if NET_VER_35 && !NET_VER_40
            _lastError.Length = 0;
        #else
            _lastError.Clear();
        #endif
            _lastError.AppendFormat("{0}", errMsg);
        }
        /// <summary>
        /// Protected overload method to set the LastError and LastErrorDetail property.
        /// </summary>
        /// <param name="errMsg">The message to set as the LastError property.</param>
        /// <param name="errDetails">The message (string) containing the details to set as the LastErrorDetail property.</param>
        protected void SetLastError(string errMsg, string errDetails)
        {
        #if NET_VER_35 && !NET_VER_40
            _lastError.Length = 0;
            _lastErrorDetail.Length = 0;
        #else
            _lastError.Clear();
            _lastErrorDetail.Clear();
        #endif
            _lastError.AppendFormat("{0}", errMsg);
            if (errDetails.Length > 0)
                _lastErrorDetail.AppendFormat("{0}", errDetails);
        }
        /// <summary>
        /// Protected ovoverloaderride method to set the LastError and LastErrorDetail property.
        /// </summary>
        /// <param name="errMsg">The message to set as the LastError property.</param>
        /// <param name="innerException">The exception that will be use to set the LastErrorDetail property.</param>
        protected void SetLastError(string errMsg, Exception innerException)
        {
        #if NET_VER_35 && !NET_VER_40
            _lastError.Length = 0;
            _lastErrorDetail.Length = 0;
        #else
            _lastError.Clear();
            _lastErrorDetail.Clear();
        #endif
            _lastError.AppendFormat("{0}", errMsg);
            if (innerException != null)
                _lastErrorDetail.AppendFormat("{0}\r\n{1}", innerException.Message, innerException.StackTrace);
        }
        /// <summary>
        /// Protected method to reset the last error properties.
        /// </summary>
        protected void ResetLastError()
        {
        #if NET_VER_35 && !NET_VER_40
            _lastError.Length = 0;
            _lastErrorDetail.Length = 0;
        #else
            _lastError.Clear();
            _lastErrorDetail.Clear();
        #endif
            _lastError.AppendFormat("No Error");

        }
        #endregion


        #region Dispose Methods
        /// <summary>
        /// Protected ready only property
        /// </summary>
        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }
        /// <summary>
        /// Dispose virtual method. If the inherited class
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose overload method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // free resources if needed
                    #if NET_VER_35 && !NET_VER_40
                        _lastError.Length = 0;
                        _lastErrorDetail.Length = 0;
                    #else
                        _lastError.Clear();
                        _lastErrorDetail.Clear();
                    #endif
                }
            }
            _isDisposed = true;
        }
        #endregion
    }
}
