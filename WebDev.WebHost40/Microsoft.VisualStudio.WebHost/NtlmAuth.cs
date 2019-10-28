using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
namespace Microsoft.VisualStudio.WebHost
{
	[SuppressUnmanagedCodeSecurity]
	internal sealed class NtlmAuth : IDisposable
	{
		private struct SecHandle
		{
			public IntPtr dwLower;
			public IntPtr dwUpper;
		}
		private struct SecBuffer
		{
			public uint cbBuffer;
			public uint BufferType;
			public IntPtr pvBuffer;
		}
		private struct SecBufferDesc
		{
			public uint ulVersion;
			public uint cBuffers;
			public IntPtr pBuffers;
		}
		private const int SEC_E_OK = 0;
		private const int SEC_I_COMPLETE_AND_CONTINUE = 590612;
		private const int SEC_I_COMPLETE_NEEDED = 590611;
		private const int SEC_I_CONTINUE_NEEDED = 590610;
		private const int SECPKG_CRED_INBOUND = 1;
		private const int SECBUFFER_VERSION = 0;
		private const int SECBUFFER_EMPTY = 0;
		private const int SECBUFFER_DATA = 1;
		private const int SECBUFFER_TOKEN = 2;
		private const int SECURITY_NETWORK_DREP = 0;
		private const int ISC_REQ_DELEGATE = 1;
		private const int ISC_REQ_MUTUAL_AUTH = 2;
		private const int ISC_REQ_REPLAY_DETECT = 4;
		private const int ISC_REQ_SEQUENCE_DETECT = 8;
		private const int ISC_REQ_CONFIDENTIALITY = 16;
		private const int ISC_REQ_USE_SESSION_KEY = 32;
		private const int ISC_REQ_PROMPT_FOR_CREDS = 64;
		private const int ISC_REQ_USE_SUPPLIED_CREDS = 128;
		private const int ISC_REQ_ALLOCATE_MEMORY = 256;
		private const int ISC_REQ_STANDARD_FLAGS = 20;
		private NtlmAuth.SecHandle _credentialsHandle;
		private bool _credentialsHandleAcquired;
		private NtlmAuth.SecHandle _securityContext;
        private bool _securityContextAcquired;
        private uint _securityContextAttributes;
		private long _timestamp;
        private NtlmAuth.SecBufferDesc _inputBufferDesc;
        private NtlmAuth.SecBuffer _inputBuffer;
        private NtlmAuth.SecBufferDesc _outputBufferDesc;
        private NtlmAuth.SecBuffer _outputBuffer;
        private bool _completed;
		private string _blob;
        private SecurityIdentifier _sid;
		public string Blob
		{
			get
			{
				return this._blob;
			}
		}
		public bool Completed
		{
			get
			{
				return this._completed;
			}
		}
		public SecurityIdentifier SID
		{
			get
			{
				return this._sid;
			}
		}
		public NtlmAuth()
		{
			int num = NtlmAuth.AcquireCredentialsHandle(null, "NTLM", 1u, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref this._credentialsHandle, ref this._timestamp);
			if (num != 0)
			{
				throw new InvalidOperationException();
			}
			this._credentialsHandleAcquired = true;
		}
		~NtlmAuth()
		{
			this.FreeUnmanagedResources();
		}
		void IDisposable.Dispose()
		{
			this.FreeUnmanagedResources();
			GC.SuppressFinalize(this);
		}
		private void FreeUnmanagedResources()
		{
			if (this._securityContextAcquired)
			{
				NtlmAuth.DeleteSecurityContext(ref this._securityContext);
			}
			if (this._credentialsHandleAcquired)
			{
				NtlmAuth.FreeCredentialsHandle(ref this._credentialsHandle);
			}
		}
		public unsafe bool Authenticate(string blobString)
		{
			this._blob = null;
            //byte[] array = Convert.FromBase64String(blobString);
            //byte[] array2 = new byte[16384];
            //unsafe
            //{
            //    fixed (IntPtr* ptr = (IntPtr*)(&this._securityContext))
            //    {
            //        fixed (IntPtr* ptr2 = (IntPtr*)(&this._inputBuffer))
            //        {
            //            fixed (IntPtr* ptr3 = (IntPtr*)(&this._outputBuffer))
            //            {
            //                fixed (IntPtr* ptr4 = (IntPtr*)(&array[0]))
            //                {
            //                    fixed (IntPtr* ptr5 = (IntPtr*)(&array2[0]))
            //                    {
            //                        IntPtr phContext = IntPtr.Zero;
            //                        if (this._securityContextAcquired)
            //                        {
            //                            phContext = (IntPtr)((void*)ptr);
            //                        }
            //                        this._inputBufferDesc.ulVersion = 0u;
            //                        this._inputBufferDesc.cBuffers = 1u;
            //                        this._inputBufferDesc.pBuffers = (IntPtr)((void*)ptr2);
            //                        this._inputBuffer.cbBuffer = (uint)array.Length;
            //                        this._inputBuffer.BufferType = 2u;
            //                        this._inputBuffer.pvBuffer = (IntPtr)((void*)ptr4);
            //                        this._outputBufferDesc.ulVersion = 0u;
            //                        this._outputBufferDesc.cBuffers = 1u;
            //                        this._outputBufferDesc.pBuffers = (IntPtr)((void*)ptr3);
            //                        this._outputBuffer.cbBuffer = (uint)array2.Length;
            //                        this._outputBuffer.BufferType = 2u;
            //                        this._outputBuffer.pvBuffer = (IntPtr)((void*)ptr5);
            //                        int num = NtlmAuth.AcceptSecurityContext(ref this._credentialsHandle, phContext, ref this._inputBufferDesc, 20u, 0u, ref this._securityContext, ref this._outputBufferDesc, ref this._securityContextAttributes, ref this._timestamp);
            //                        if (num != 590610)
            //                        {
            //                            bool result;
            //                            if (num == 0)
            //                            {
            //                                IntPtr zero = IntPtr.Zero;
            //                                num = NtlmAuth.QuerySecurityContextToken(ref this._securityContext, ref zero);
            //                                if (num == 0)
            //                                {
            //                                    try
            //                                    {
            //                                        using (WindowsIdentity windowsIdentity = new WindowsIdentity(zero))
            //                                        {
            //                                            this._sid = windowsIdentity.User;
            //                                        }
            //                                    }
            //                                    finally
            //                                    {
            //                                        NtlmAuth.CloseHandle(zero);
            //                                    }
            //                                    this._completed = true;
            //                                    goto IL_1C2;
            //                                }
            //                                result = false;
            //                            }
            //                            else
            //                            {
            //                                result = false;
            //                            }
            //                            return result;
            //                        }
            //                        this._securityContextAcquired = true;
            //                        this._blob = Convert.ToBase64String(array2, 0, (int)this._outputBuffer.cbBuffer);
            //                    IL_1C2: ;
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
			return true;
		}
		[DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
		private static extern int AcquireCredentialsHandle(string pszPrincipal, string pszPackage, uint fCredentialUse, IntPtr pvLogonID, IntPtr pAuthData, IntPtr pGetKeyFn, IntPtr pvGetKeyArgument, ref NtlmAuth.SecHandle phCredential, ref long ptsExpiry);
		[DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
		private static extern int FreeCredentialsHandle(ref NtlmAuth.SecHandle phCredential);
		[DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
		private static extern int AcceptSecurityContext(ref NtlmAuth.SecHandle phCredential, IntPtr phContext, ref NtlmAuth.SecBufferDesc pInput, uint fContextReq, uint TargetDataRep, ref NtlmAuth.SecHandle phNewContext, ref NtlmAuth.SecBufferDesc pOutput, ref uint pfContextAttr, ref long ptsTimeStamp);
		[DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
		private static extern int DeleteSecurityContext(ref NtlmAuth.SecHandle phContext);
		[DllImport("SECUR32.DLL", CharSet = CharSet.Unicode)]
		private static extern int QuerySecurityContextToken(ref NtlmAuth.SecHandle phContext, ref IntPtr phToken);
		[DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
		private static extern int CloseHandle(IntPtr phToken);
	}
}
