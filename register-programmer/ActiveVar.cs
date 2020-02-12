using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace register_programmer
{
    class ActiveVar<T> : INotifyPropertyChanged
    {
        /*public ActiveVar()
        {
            this._value = new T();
        }

        public ActiveVar(T t)
        {
            this._value = t;
        }*/

        public T Value
        {
            get { return this._value; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(this._value, value))
                    return;

                this._value = value;
                this.OnPropertyChanged(new PropertyChangedEventArgs("Value"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
                handler(this, e);
        }

        private T _value;


    }

    class MyMath
    {
        public decimal Power(decimal b, decimal p)
        {
            decimal res = 1;
            for (; p > 0; p--)
            {
                res *= b;
            }
            return res;
        }
    }

    

    class EventArgsData : EventArgs
    {
        public EventArgsData(object data)
        {
            this._data = data;
        }
        
        public object Data { get { return this._data; } }
        private object _data;
    }
}
