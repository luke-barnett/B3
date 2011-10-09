using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Data;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace IndiaTango.Models
{
    /// <summary>
    /// Original calculator code sourced from:http://www.c-sharpcorner.com/UploadFile/mgold/CodeDomCalculator08082005003253AM/CodeDomCalculator.aspx
    /// Author: Mike Gold
    /// </summary>
    public class FormulaEvaluator
    {
        readonly ArrayList _mathMembers = new ArrayList();
        readonly Hashtable _mathMembersMap = new Hashtable();
        StringBuilder _source = new StringBuilder();
        readonly ICodeCompiler _compiler;
        readonly CompilerParameters _parms;
        private List<SensorVariable> _sensorStates;
    	private List<SensorVariable> _variablesUsed;
    	private List<SensorVariable> _variablesAssignedTo;
        private string loopStartCode = "";
        private string loopEndCode = "";
        private int _interval;

        #region PublicMethods
        public FormulaEvaluator(List<Sensor> sensorStates, int interval)
        {
            //Create the compiler
            CodeDomProvider codeProvider = null;
            codeProvider = new CSharpCodeProvider();
            _compiler = codeProvider.CreateCompiler();
            _interval = interval;
            _sensorStates = SensorVariable.CreateSensorVariablesFromSensors(sensorStates);

            //add compiler parameters and assembly references
            _parms = new CompilerParameters();
            _parms.CompilerOptions = "/target:library /optimize";
            _parms.GenerateExecutable = false;
            _parms.GenerateInMemory = true;
            _parms.IncludeDebugInformation = false;
            _parms.ReferencedAssemblies.Add("mscorlib.dll");
            _parms.ReferencedAssemblies.Add("System.dll");
            _parms.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            _parms.ReferencedAssemblies.Add("System.Core.dll");
            //_parms.ReferencedAssemblies.Add("System.Collections.Generic");
            _parms.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            Debug.WriteLine(Assembly.GetExecutingAssembly().Location);
            //Add any aditional references needed
            //foreach (string refAssembly in IndiaTango )
              //  _parms.ReferencedAssemblies.Add(refAssembly);

            GetMathMemberNames();
        }

        public bool ParseFormula(string formula)
        {
            Regex regularExpression = new Regex("^[0-9x/.=/-/+/*/^/(/)/t (t/..+)]*$");

            return  regularExpression.IsMatch(formula);
        }

        public Formula CompileFormula(string formula)
        {
            // change evaluation string to pick up Math class members
            formula = RefineEvaluationString(formula);

            //Get the varibles used in formula
            _variablesUsed = new List<SensorVariable>();
            foreach (SensorVariable sensorVariable in _sensorStates)
            {
                if (formula.Contains(sensorVariable.VariableName + " ")  || formula.Contains(" " + sensorVariable.VariableName))
                {
                    _variablesUsed.Add(sensorVariable);
                }
            }

            //Get the variables assigned to in the formula
            _variablesAssignedTo = new List<SensorVariable>();
            foreach (SensorVariable sensorVariable in _variablesUsed)
            {
                if (formula.Contains(sensorVariable.VariableName + " ="))
                {
                    _variablesAssignedTo.Add(sensorVariable);
                }
            }

            //Generate start loopp code
            loopStartCode = "for(DateTime time = startTime; time <= endTime ; time = time.AddMinutes(" + _interval + "))\n" +
                            "{\n";

            for (int v = 0; v < _sensorStates.Count; v++)
            {
                if (_variablesUsed.Contains(_sensorStates[v]))
                {
                    loopStartCode += "float " + _sensorStates[v].VariableName + " = 0;\n";
                    loopStartCode += "sensorStates[" + v + "].Sensor.CurrentState.Values.TryGetValue(time,out " +
                                     _sensorStates[v].VariableName + ");\n";
                }
            }

            //Generate the code for the end of the loop
            loopEndCode = "";

            for (int v = 0; v < _sensorStates.Count; v++)
            {
                if (_variablesAssignedTo.Contains(_sensorStates[v]))
                {
                    loopEndCode += "sensorStates[" + v + "].Sensor.CurrentState.Values[time] = " +
                                   _sensorStates[v].VariableName + ";\n";
                }
            }

            loopEndCode += "}\n";

            // build the class using codedom
            BuildClass(formula);

			//actually compile the code
			CompilerResults results = _compiler.CompileAssemblyFromSource(_parms, _source.ToString());
			
			//Do we have any compiler errors?
			if (results.Errors.Count > 0)
			{
				foreach (CompilerError error in results.Errors)
					Console.WriteLine("Compile Error. Line " +error.Line + ":" + error.ErrorText);
			}

            Console.WriteLine("...........................\r\n");
            Console.WriteLine(_source.ToString());

            return new Formula(results,_variablesUsed);
        }

        public List<Sensor> EvaluateFormula(Formula formula,  DateTime startTime, DateTime endTime, bool skipMissingValues)
        {
            if (startTime >= endTime)
                throw new ArgumentException("End time must be greater than start time");

            // if the code compiled okay,
            // run the code using the new assembly (which is inside the results)
			if (formula.CompilerResults != null && formula.CompilerResults.CompiledAssembly != null)
            {
				//Foreach variable assigned to, give it a new sensor state
            	foreach (var sensorVariable in _variablesAssignedTo)
            		sensorVariable.Sensor.AddState(sensorVariable.Sensor.CurrentState.Clone());

                // run the evaluation function
				return RunCode(formula.CompilerResults, startTime, endTime, skipMissingValues);
            }
            else
            {
                Debug.WriteLine("Could not evaluate formula");
                return null;
            }
        }

        #endregion


        #region PrivateMethods

        /// <summary>
        /// Change evaluation string to use .NET Math library
        /// </summary>
        /// <param name="eval">evaluation expression</param>
        /// <returns></returns>
        private string RefineEvaluationString(string eval)
        {
            // look for regular expressions with only letters
            Regex regularExpression = new Regex("[a-zA-Z_]+");

            // track all functions and constants in the evaluation expression we already replaced
            ArrayList replacelist = new ArrayList();

            // find all alpha words inside the evaluation function that are possible functions
            MatchCollection matches = regularExpression.Matches(eval);
            foreach (Match m in matches)
            {
                // if the word is found in the math member map, add a Math prefix to it
                bool isContainedInMathLibrary = _mathMembersMap[m.Value.ToUpper()] != null;
                if (replacelist.Contains(m.Value) == false && isContainedInMathLibrary)
                {
                    eval = eval.Replace(m.Value, "Math." + _mathMembersMap[m.Value.ToUpper()]);
                }

                // we matched it already, so don't allow us to replace it again
                replacelist.Add(m.Value);
            }

            // return the modified evaluation string

            eval = eval.Replace("=", "= (float)");

            return eval;
        }

        private void GetMathMemberNames()
        {
            // get a reflected assembly of the System assembly
            Assembly systemAssembly = Assembly.GetAssembly(typeof(System.Math));
            try
            {
                //cant call the entry method if the assembly is null
                if (systemAssembly != null)
                {
                    //Use reflection to get a reference to the Math class

                    Module[] modules = systemAssembly.GetModules(false);
                    Type[] types = modules[0].GetTypes();

                    //loop through each class that was defined and look for the first occurrance of the Math class
                    foreach (Type type in types)
                    {
                        if (type.Name == "Math")
                        {
                            // get all of the members of the math class and map them to the same member
                            // name in uppercase
                            MemberInfo[] mis = type.GetMembers();
                            foreach (MemberInfo mi in mis)
                            {
                                _mathMembers.Add(mi.Name);
                                _mathMembersMap[mi.Name.ToUpper()] = mi.Name;
                            }
                        }
                        //if the entry point method does return in Int32, then capture it and return it
                    }


                    //if it got here, then there was no entry point method defined.  Tell user about it
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:  An exception occurred while executing the script", ex);
            }
        }

        /// <summary>
        /// Runs the Calculate method in our on-the-fly assembly
        /// </summary>
        /// <param name="results"></param>
		private List<Sensor> RunCode(CompilerResults results, DateTime startTime, DateTime endTime, bool skipMissingValues)
        {
            Assembly executingAssembly = results.CompiledAssembly;
            try
            {
                //cant call the entry method if the assembly is null
                if (executingAssembly != null)
                {
                    object assemblyInstance = executingAssembly.CreateInstance("IndiaTango.Calculator");
                    //Use reflection to call the static Main function

                    Module[] modules = executingAssembly.GetModules(false);
                    Type[] types = modules[0].GetTypes();

                    //loop through each class that was defined and look for the first occurrance of the entry point method
                    foreach (Type type in types)
                    {
                        MethodInfo[] mis = type.GetMethods();
                        foreach (MethodInfo mi in mis)
                        {
                            if (mi.Name == "ApplyFormula")
                            {
								return (List<Sensor>)mi.Invoke(assemblyInstance, new object[] { _sensorStates, startTime, endTime, skipMissingValues});
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Common.ShowMessageBoxWithException("Error", "An exception occurred while executing the script", false, true, ex);
                Console.WriteLine("Error:  An exception occurred while executing the script: \n" + ex);
            }

            return null;
        }

        CodeMemberField FieldVariable(string fieldName, string typeName, MemberAttributes accessLevel)
        {
            CodeMemberField field = new CodeMemberField(typeName, fieldName);
            field.Attributes = accessLevel;
            return field;
        }

        CodeMemberField FieldVariable(string fieldName, Type type, MemberAttributes accessLevel)
        {
            CodeMemberField field = new CodeMemberField(type, fieldName);
            field.Attributes = accessLevel;
            return field;
        }

        /// <summary>
        /// Very simplistic getter/setter properties
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="internalName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private CodeMemberProperty MakeProperty(string propertyName, string internalName, Type type)
        {
            CodeMemberProperty myProperty = new CodeMemberProperty();
            myProperty.Name = propertyName;
            myProperty.Comments.Add(new CodeCommentStatement(String.Format("The {0} property is the returned result", propertyName)));
            myProperty.Attributes = MemberAttributes.Public;
            myProperty.Type = new CodeTypeReference(type);
            myProperty.HasGet = true;
            myProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), internalName)));

            myProperty.HasSet = true;
            myProperty.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), internalName),
                    new CodePropertySetValueReferenceExpression()));

            return myProperty;
        }

        /// <summary>
        /// Main driving routine for building a class
        /// </summary>
        private void BuildClass(string expression)
        {
            // need a string to put the code into
            _source = new StringBuilder();
            StringWriter sw = new StringWriter(_source);

            //Declare your provider and generator
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeGenerator generator = codeProvider.CreateGenerator(sw);
            CodeGeneratorOptions codeOpts = new CodeGeneratorOptions();
            

            //Setup the namespace and imports
            CodeNamespace myNamespace = new CodeNamespace("IndiaTango");
            myNamespace.Imports.Add(new CodeNamespaceImport("System"));
            myNamespace.Imports.Add(new CodeNamespaceImport("System.Windows.Forms"));
            myNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            myNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            myNamespace.Imports.Add(new CodeNamespaceImport("IndiaTango.Models"));
            //myNamespace.Imports.Add(new CodeNamespaceImport("System.Math"));
            //Build the class declaration and member variables			
            CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration();
            classDeclaration.IsClass = true;
            classDeclaration.Name = "Calculator";
            classDeclaration.Attributes = MemberAttributes.Public;

            //default constructor
            CodeConstructor defaultConstructor = new CodeConstructor();
            defaultConstructor.Attributes = MemberAttributes.Public;
            defaultConstructor.Comments.Add(new CodeCommentStatement("Default Constructor for class", true));
            classDeclaration.Members.Add(defaultConstructor);

            //Our Calculate Method
            CodeMemberMethod myMethod = new CodeMemberMethod();
            myMethod.Name = "ApplyFormula";
            myMethod.ReturnType = new CodeTypeReference(typeof(List<Sensor>));
            myMethod.Comments.Add(new CodeCommentStatement("Apply a formula across a dataset", true));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(List<SensorVariable>)), "sensorStates"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(DateTime)), "startTime"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(DateTime)), "endTime"));
			myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "skipMissingValues"));
            myMethod.Attributes = MemberAttributes.Public;
            myMethod.Statements.Add(new CodeSnippetExpression(loopStartCode));

            //Add the user specified code
            myMethod.Statements.Add(new CodeSnippetExpression(expression));

            myMethod.Statements.Add(new CodeSnippetExpression(loopEndCode));
            myMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("SensorVariable.CreateSensorsFromSensorVariables(sensorStates)")));
            classDeclaration.Members.Add(myMethod);

            //write code
            myNamespace.Types.Add(classDeclaration);
            generator.GenerateCodeFromNamespace(myNamespace, sw, codeOpts);
            sw.Flush();
            sw.Close();
        }

        #endregion
    }

	public class Formula
	{
		private CompilerResults _results;
		private List<SensorVariable> _sensors;

		public Formula(CompilerResults results, List<SensorVariable> sensorsUsed)
		{
			_results = results;
			_sensors = sensorsUsed;
		}

		public CompilerResults CompilerResults
		{
			get { return _results; }
		}

		public List<SensorVariable> SensorsUsed
		{
			get { return _sensors; }
		}

		public bool IsValid
		{
			get { return _results != null && _results.Errors.Count == 0 && _results.CompiledAssembly != null; }
		}
	}
}
