using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{
	public class API
	{

		public Connection connection;
		public Contacts contacts;
		public Analytics analytics;
		public Alliance alliance;
		public Clan clan;
		public Inbox inbox;
		public Squad squad;

		public API ()
		{
			contacts = new Contacts () { api = this };
			analytics = new Analytics (){ api = this };
			alliance = new Alliance (){ api = this };
			clan = new Clan (){ api = this };
			inbox = new Inbox (){ api = this };
			squad = new Squad () { api = this };
		}

		public void Join (string room)
		{
			Send ("join", room);
		}
        
		public void Leave (string room)
		{
			Send ("leave", room);
		}
        
		public void Say (string room, string cmd, params object[] args)
		{
			Send ("say", room, cmd, args);
		}
        
		public Request Register (string handle, string email, string password)
		{
            
			var hash = SHA256 (password).ToLower ();
			return Request ("member.register", handle, email, hash);
		}
        
		public Request Authenticate (string email, string password)
		{
			var hash = SHA256 (password).ToLower ();
            
			hash = connection.UID + hash;
			hash = SHA256 (hash).ToLower ();
			return Request ("member.authenticate", email, hash);
		}
        
		public void RequestPasswordReset (string email)
		{
			Send ("member.password_reset_request", email);
		}
        
		public Request ResetPassword (string email, string token, string password)
		{
			password = SHA256 (password);
			return Request ("member.password_reset", email, token, password);
		}

		public void Send (string name, params object[] args)
		{
			connection.Send (name, args);
		}

		public Request Request (string name, params object[] args)
		{
			return connection.Request (name, args);
		}

		string SHA256 (string phrase)
		{
			if (phrase == null)
				return null;
			var encoder = new System.Text.UTF8Encoding ();
			var sha256Hasher = new System.Security.Cryptography.SHA256Managed ();
			var hashedDataBytes = sha256Hasher.ComputeHash (encoder.GetBytes (phrase));
			return ByteArrayToHexString (hashedDataBytes);
		}
        
		string ByteArrayToHexString (byte[] array)
		{
			StringBuilder stringBuilder = new StringBuilder (array.Length * 2);
			byte[] array2 = array;
			for (int i = 0; i < array2.Length; i++) {
				byte b = array2 [i];
				stringBuilder.AppendFormat ("{0:x2}", b);
			}
			return stringBuilder.ToString ().PadLeft (32, '0');
		}

	}

}
