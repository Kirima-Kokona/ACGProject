//Copied from PBDLearn
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{

    [System.Serializable]
    public class ClothSetting
    {

        [Range(0.01f,1)]
        [SerializeField]
        private float _density = 1;

        [Range(1,10)]
        [SerializeField]
        private int _constraintSolverIteratorCount = 2;

        [Range(0.01f,1f)]
        [SerializeField]
        private float _compressStiffness = 0.8f;

        [Range(0.01f,1f)]
        [SerializeField]
        private float _stretchStiffness = 0.8f;

        [Range(0.01f,1)]
        [SerializeField]
        private float _bendStiffness = 0.1f;

        [Range(0.01f,10)]
        [SerializeField]
        private float _damper = 1;

        public float density{
            get{
                return _density;
            }
        }

        public int constraintSolverIteratorCount{
            get{
                return _constraintSolverIteratorCount;
            }
        }

        public float compressStiffness{
            get{
                return this._compressStiffness;
            }
        }

        public float stretchStiffness{
            get{
                return _stretchStiffness;
            }
        }

        public float bendStiffness{
            get{
                return _bendStiffness;
            }
        }

        public float damper{
            get{
                return _damper;
            }
        }


    }
}
