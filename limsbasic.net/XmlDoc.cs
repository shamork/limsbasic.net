using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace LabObjects.LimsBasicNet
{
    /// <summary>
    /// 
    /// </summary>
    public class XmlDoc : LimsBasicNetBase, IDisposable
    {
        #region private fields
        //private bool _isDisposed = false;
        private System.Xml.XmlDocument _xml;
        //private System.Xml.Schema.XmlSchemaSet _schemas;
        private string _validationMsgs;

        #endregion

        //delegate ValidationEventHandler DocValidationHandler();

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public XmlDoc()
        {
            InitData();
        }

        //public XmlDoc(string xmlFragmentOrFileName)
        //{

        //}
        //public XmlDoc( string xmlFragmentOrFileName,  string [] schemaRefs )
        //{

        //}
        #endregion

        #region Public Properties
        /// <summary>
        /// The xml document as a string
        /// </summary>
        public string XmlString
        {
            get { return _xml.DocumentElement != null ? _xml.DocumentElement.InnerXml : ""; }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Load XML from file name method
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Load(string fileName)
        {
            return LoadXmlFile(fileName);
        }
        /// <summary>
        /// Save XML File
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Save(string fileName)
        {
            return SaveToFile(fileName);
        }
        /// <summary>
        /// overload method for Save method
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="preserveWhitespace"></param>
        /// <returns></returns>
        public bool Save(string fileName, bool preserveWhitespace)
        {
            _xml.PreserveWhitespace = true;
            return SaveToFile(fileName);
        }
        /// <summary>
        /// The Xml.XmlDocument object
        /// </summary>
        /// <returns></returns>
        public XmlDocument GetXmlDocument()
        {
            return _xml;
        }
        /// <summary>
        /// Method to validate the current xnl document against one or more schemas
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            bool status = false;
            try
            {
                _validationMsgs = "";
                if (_xml.Schemas.Count == 0)
                {

                }
                ValidationEventHandler eventHandler = new ValidationEventHandler(DocValidationHandler);
                _xml.Validate(eventHandler);
                status = true;
            }
            catch (XmlSchemaValidationException ex)
            {
                SetLastError(string.Format("XML Document Validation Error: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("XML Document Validation Error: {0}", ex.Message));
            }
            return status;
        }
        /// <summary>
        /// Method to add a Schema (XSD) document to the schema collection for the designated target namespace
        /// </summary>
        /// <param name="targetNamespace">Schema target namespace</param>
        /// <param name="schemaUri">URI to the schema file (e.g., http://, c:\FilePath\schemafile.xsd)</param>
        /// <returns></returns>
        public bool AddSchema(string targetNamespace, string schemaUri)
        {
            bool status = false;
            try
            {
                _xml.Schemas.Add(targetNamespace, schemaUri);
                status = true;
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message);
            }
            return status;
        }
        /// <summary>
        /// Method to remove a schema specified by a target namespace from the schemas collection
        /// </summary>
        /// <param name="targetNamespace"></param>
        /// <returns></returns>
        public bool RemoveSchema(string targetNamespace)
        {
            bool status = false;
            try
            {
                XmlSchemaSet schemas = _xml.Schemas;
                if (_xml.Schemas.Contains(targetNamespace))
                {
                    foreach (XmlSchema s in schemas.Schemas(targetNamespace))
                        _xml.Schemas.Remove(s);
                }
                else
                {
                    throw new Exception(string.Format("No Schemas found for Target Namespace: {0}", targetNamespace));
                }

                status = true;
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message);
            }
            return status;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Helper method to initlize the object
        /// </summary>
        private void InitData()
        {
            _xml = new XmlDocument();
        }
        /// <summary>
        /// Method to load a XML file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool LoadXmlFile(string fileName)
        {
            bool status = false;
            try
            {
                _xml.Load(fileName);
                status = true;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                SetLastError(string.Format("Unable to Load XML Document: {0} - {1}", fileName, ex.Message));
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Unable to Load XML Document: {0} - {1}", fileName, ex.Message), ex.InnerException);
            }
            return status;
        }
        /// <summary>
        /// Method to load a Xml Document from a String
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private bool LoadFromString(string xml)
        {
            bool status = false;
            try
            {
                _xml.LoadXml(xml);
                status = true;
            }
            catch (Exception ex)
            {
                SetLastError(string.Format("Unable to Load XML Document: {0}", ex.Message), ex.InnerException);
            }
            return status;
        }
        /// <summary>
        /// Helper method to save a xml file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool SaveToFile(string fileName)
        {
            bool status = false;
            try
            {
                _xml.Save(fileName);
                status = true;
            }
            catch (XmlException ex)
            {
                SetLastError(ex.Message, ex.InnerException);
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message, ex.InnerException);
            }
            return status;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Schema validation event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DocValidationHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    //_validationMsgs += "Error: " + e.Message + "\r\n";
                    break;
                case XmlSeverityType.Warning:
                    //_validationMsgs += "Warning: " + e.Message + "\r\n";
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Disposal
        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
        /// <summary>
        /// Dispose Method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual new void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    if (_xml != null)
                    {
                        _xml = null;
                                
                    }
                }
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// XmlDoc Finalization Method
        /// </summary>
        ~XmlDoc()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
