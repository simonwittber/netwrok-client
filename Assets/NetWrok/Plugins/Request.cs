using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniExtensions;
using System.Text;
using System.Linq;

namespace NetWrok
{
	public class Request
	{
        public string id;
        public bool isDone = false;

        object _result;
        string _error;
        public object Result {
            get {
                if(_error != null) {
                    throw new System.InvalidOperationException("Result is an Exception: " + _error);
                } 
                return _result;
            }
            set {
                _result = value;
                _error = null;
            }
        }

        public string Error {
            get {
                return _error;
            }
            set {
                _result = null;
                _error = value;
            }
        }

        Connection connection;

        public Request(Connection connection, Message msg) {
            this.connection = connection;
            this.id = msg.id;
        }

        public Coroutine Wait() {
            return connection.StartCoroutine(_Wait());
        }

        IEnumerator _Wait() {
            while(!isDone) {
                yield return null;
            }
        }

	}

}