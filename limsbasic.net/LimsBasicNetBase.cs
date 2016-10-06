using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabObjects.LimsBasicNet
{
    /// <summary>
    /// LimsBasicNetBase - base class for limsbase.net class library.
    /// </summary>
    public abstract class LimsBasicNetBase : IDisposable
    {
        #region Private fields
        private StringBuilder _lastError;
        private StringBuilder _lastErrorDetail;
        private bool _isDisposed = false;
        #endregion



        #region Constructors
        internal LimsBasicNetBase()
        {
            _lastError = new StringBuilder("No Error");
            _lastErrorDetail = new StringBuilder("");
            ResetLastError();
        }
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
            _lastError.Clear();
            _lastError.AppendFormat("{0}", errMsg);
        }
        /// <summary>
        /// Protected overload method to set the LastError and LastErrorDetail property.
        /// </summary>
        /// <param name="errMsg">The message to set as the LastError property.</param>
        /// <param name="errDetails">The message (string) containing the details to set as the LastErrorDetail property.</param>
        protected void SetLastError(string errMsg, string errDetails)
        {
            _lastError.Clear();
            _lastError.AppendFormat("{0}", errMsg);
            _lastErrorDetail.Clear();
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
            _lastError.Clear();
            _lastError.AppendFormat("{0}", errMsg);
            _lastErrorDetail.Clear();
            if (innerException != null)
                _lastErrorDetail.AppendFormat("{0}\r\n{1}", innerException.Message, innerException.StackTrace);
        }
        /// <summary>
        /// Protected method to reset the last error properties.
        /// </summary>
        protected void ResetLastError()
        {
            _lastError.Clear();
            _lastError.AppendFormat("No Error");
            _lastErrorDetail.Clear();
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
                    _lastError.Clear();
                    _lastErrorDetail.Clear();
                }
            }
            _isDisposed = true;
        }
        #endregion
    }
}
