using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Text;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IndiaTango.Models
{
    /// <summary>
    /// Original calculator code sourced from:http://www.c-sharpcorner.com/UploadFile/mgold/CodeDomCalculator08082005003253AM/CodeDomCalculator.aspx
    /// Author: Mike Gold
    /// </summary>
    public class FormulaEvaluator
    {
        private readonly CodeDomProvider _codeProvider;
        private readonly List<SensorVariable> _sensorVariables;
        private readonly CompilerParameters _compilerParameters;

        private readonly ArrayList _mathMembers;
        private readonly Hashtable _mathMembersMap;

        private StringBuilder _source;

        private string _loopCode = "";

        #region PublicMethods

        public FormulaEvaluator(IEnumerable<Sensor> sensors)
        {
            //Build the code provider
            _codeProvider = new CSharpCodeProvider();

            //Retrieve the sensor variables
            _sensorVariables = sensors.Select(x => x.Variable).ToList();

            //Create the compiler parameters
            _compilerParameters = new CompilerParameters
                                      {
                                          CompilerOptions = "/target:library /optimize",
                                          GenerateExecutable = false,
                                          GenerateInMemory = true,
                                          IncludeDebugInformation = false
                                      };

            //Add the needed references
            _compilerParameters.ReferencedAssemblies.Add("System.dll");
            _compilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            _compilerParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

            //Build Math Objects
            _mathMembers = new ArrayList();
            _mathMembersMap = new Hashtable();

            //Populate Math Objects
            GetMathMemberNames();

            //Build String Builder
            _source = new StringBuilder();
        }

        /// <summary>
        /// Compiles a formula
        /// </summary>
        /// <param name="formula">The formula</param>
        /// <returns>The compiled result</returns>
        public Formula CompileFormula(string formula)
        {
            //Get variables used in the formula
            var variablesUsed = _sensorVariables.Where(sensorVariable => sensorVariable != null && formula.Contains(string.Format(" {0} ", sensorVariable.VariableName))).ToList();

            var variableAssignedTo = _sensorVariables.FirstOrDefault(sensorVariable => sensorVariable != null && formula.StartsWith(string.Format("{0} =", sensorVariable.VariableName)));

            if (variableAssignedTo == null)
                return new Formula(_codeProvider.CompileAssemblyFromSource(_compilerParameters, "a"), variablesUsed, variableAssignedTo);

            //Fill out the formula to pick up the Math class members
            formula = ExtendForMathClassMembers(formula);

            _loopCode = "var state = variableAssignedTo.Sensor.CurrentState.Clone();\r\n";

            _loopCode += "for(var time = startTime; time <= endTime; time = time.AddMinutes(variableAssignedTo.Sensor.Owner.DataInterval))\r\n{\r\n";

            for (var i = 0; i < _sensorVariables.Count; i++)
            {
                if (!variablesUsed.Contains(_sensorVariables[i]))
                    continue;
                _loopCode += string.Format("float {0} = float.NaN;\r\n", _sensorVariables[i].VariableName);
                _loopCode += string.Format("if(!sensorVariables[{0}].Sensor.CurrentState.Values.TryGetValue(time, out {1})) continue;\r\n", i, _sensorVariables[i].VariableName);
            }

            _loopCode += "if(!skipMissingValues || state.Values.ContainsKey(time))\r\n" +
                           "{\r\n" +
                           "state.Values[time] " + formula + "\r\n" +
                           "state.AddToChanges(time,reason.ID);\r\n" +
                           "}\r\n" +
                           "}\r\n";

            BuildClass();

            var compilerResults = _codeProvider.CompileAssemblyFromSource(_compilerParameters, _source.ToString());

            if (compilerResults.Errors.Count > 0)
            {
                foreach (CompilerError error in compilerResults.Errors)
                    Debug.WriteLine("Compile Error. Line " + error.Line + ":" + error.ErrorText);
            }

            Debug.WriteLine("...........................\r\n");
            Debug.WriteLine(_source.ToString());

            return new Formula(compilerResults, variablesUsed, variableAssignedTo);
        }

        /// <summary>
        /// Evaluates a formula
        /// </summary>
        /// <param name="formula">The formula to evaluate</param>
        /// <param name="startTime">The start time to evaluate from</param>
        /// <param name="endTime">The end time to stop evaluating from</param>
        /// <param name="skipMissingValues">Whether or not to skip missing values</param>
        /// <param name="reason">The reason for the evaluation</param>
        /// <returns>The sensor used and the newly evaulated state</returns>
        public KeyValuePair<Sensor, SensorState> EvaluateFormula(Formula formula, DateTime startTime, DateTime endTime, bool skipMissingValues, ChangeReason reason)
        {
            if (startTime >= endTime)
                throw new ArgumentException("End time must be greater than start time");

            // if the code compiled okay,
            // run the code using the new assembly (which is inside the results)
            if (formula.CompilerResults != null && formula.CompilerResults.CompiledAssembly != null)
            {
                // run the evaluation function
                return RunCode(formula, startTime, endTime, skipMissingValues, reason);
            }

            Debug.WriteLine("Could not evaluate formula");
            return new KeyValuePair<Sensor, SensorState>(null, null);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the math members
        /// </summary>
        private void GetMathMemberNames()
        {
            // get a reflected assembly of the System assembly
            var systemAssembly = Assembly.GetAssembly(typeof(Math));
            try
            {
                if (systemAssembly == null)
                    return;

                //Use reflection to get a reference to the Math class
                var modules = systemAssembly.GetModules(false);
                var types = modules[0].GetTypes();

                //loop through each class that was defined and look for the first occurrance of the Math class
                foreach (var mi in from type in types where type.Name == "Math" select type.GetMembers() into mis from mi in mis select mi)
                {
                    _mathMembers.Add(mi.Name);
                    _mathMembersMap[mi.Name.ToUpper()] = mi.Name;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error:  An exception occurred while executing the script\n" + ex);
            }
        }

        /// <summary>
        /// Extends a formula for the math class
        /// </summary>
        /// <param name="formula">The formula to extend</param>
        /// <returns>The extended formula</returns>
        private string ExtendForMathClassMembers(string formula)
        {
            var regularExpression = new Regex("[a-zA-Z_]+");

            var matches = regularExpression.Matches(formula);

            var replacelist = new ArrayList();

            foreach (Match match in matches)
            {
                // if the word is found in the math member map, add a Math prefix to it
                var isContainedInMathLibrary = _mathMembersMap[match.Value.ToUpper()] != null;
                if (replacelist.Contains(match.Value) == false && isContainedInMathLibrary)
                {
                    formula = formula.Replace(match.Value, "Math." + _mathMembersMap[match.Value.ToUpper()]);
                }

                // we matched it already, so don't allow us to replace it again
                replacelist.Add(match.Value);
            }

            //Make sure all values are cast back as floats
            formula = formula.Replace("=", "= (float)(");
            formula = formula.Substring(formula.IndexOf('='));

            var numbers = new Regex(@"(-)?[0-9]+(\.[0-9]+)?");
            formula = numbers.Replace(formula, MakeFloat);

            //Add semicolon at end
            formula = formula + ");";
            return formula;
        }

        /// <summary>
        /// Makes a matched number a float
        /// </summary>
        /// <param name="m">The matched number</param>
        /// <returns>The float of the matched number</returns>
        private static string MakeFloat(Match m)
        {
            var str = m.ToString();

            return str + "f";
        }

        /// <summary>
        /// Builds the class
        /// </summary>
        private void BuildClass()
        {
            // need a string to put the code into
            _source = new StringBuilder();
            var sw = new StringWriter(_source);

            //Declare your provider and generator
            var codeProvider = new CSharpCodeProvider();
            var generator = codeProvider.CreateGenerator(sw);
            var codeOpts = new CodeGeneratorOptions();


            //Setup the namespace and imports
            var myNamespace = new CodeNamespace("IndiaTango");
            myNamespace.Imports.Add(new CodeNamespaceImport("System"));
            myNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            myNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            myNamespace.Imports.Add(new CodeNamespaceImport("IndiaTango.Models"));

            //Build the class declaration and member variables			
            var classDeclaration = new CodeTypeDeclaration { IsClass = true, Name = "Calculator", Attributes = MemberAttributes.Public };

            //default constructor
            var defaultConstructor = new CodeConstructor { Attributes = MemberAttributes.Public };
            defaultConstructor.Comments.Add(new CodeCommentStatement("Default Constructor for class", true));
            classDeclaration.Members.Add(defaultConstructor);

            //Our Calculate Method
            var myMethod = new CodeMemberMethod { Name = "ApplyFormula", ReturnType = new CodeTypeReference(typeof(KeyValuePair<Sensor, SensorState>)) };
            myMethod.Comments.Add(new CodeCommentStatement("Apply a formula across a dataset", true));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(List<SensorVariable>)), "sensorVariables"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(SensorVariable)), "variableAssignedTo"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(DateTime)), "startTime"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(DateTime)), "endTime"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "skipMissingValues"));
            myMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(ChangeReason)), "reason"));
            myMethod.Attributes = MemberAttributes.Public;

            //Add the user specified code
            myMethod.Statements.Add(new CodeSnippetExpression(_loopCode));

            myMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("new KeyValuePair<Sensor,SensorState>(variableAssignedTo.Sensor,state)")));
            classDeclaration.Members.Add(myMethod);

            //write code
            myNamespace.Types.Add(classDeclaration);
            generator.GenerateCodeFromNamespace(myNamespace, sw, codeOpts);
            sw.Flush();
            sw.Close();
        }

        /// <summary>
        /// Runs the formula
        /// </summary>
        /// <param name="formula">The formula to run</param>
        /// <param name="startTime">The start time to evaluate from</param>
        /// <param name="endTime">The end time to stop evaluating from</param>
        /// <param name="skipMissingValues">Whether or not to skip missing values</param>
        /// <param name="reason">The reason for the evaluation</param>
        /// <returns>The sensor used and the newly evaulated state</returns>
        private KeyValuePair<Sensor, SensorState> RunCode(Formula formula, DateTime startTime, DateTime endTime, bool skipMissingValues, ChangeReason reason)
        {
            var executingAssembly = formula.CompilerResults.CompiledAssembly;
            try
            {
                //cant call the entry method if the assembly is null
                if (executingAssembly != null)
                {
                    var assemblyInstance = executingAssembly.CreateInstance("IndiaTango.Calculator");
                    //Use reflection to call the static Main function

                    var modules = executingAssembly.GetModules(false);
                    var types = modules[0].GetTypes();

                    //loop through each class that was defined and look for the first occurrance of the entry point method
                    foreach (var mi in from type in types select type.GetMethods() into mis from mi in mis where mi.Name == "ApplyFormula" select mi)
                    {
                        return (KeyValuePair<Sensor, SensorState>)mi.Invoke(assemblyInstance, new object[] { _sensorVariables, formula.SensorAppliedTo, startTime, endTime, skipMissingValues, reason });
                    }

                }
            }
            catch (Exception ex)
            {
                Common.ShowMessageBoxWithException("Error", "An exception occurred while executing the script", false, true, ex);
                Debug.WriteLine("Error:  An exception occurred while executing the script: \n" + ex);
            }

            return new KeyValuePair<Sensor, SensorState>(null, null);
        }

        #endregion
    }

    public class Formula
    {
        public Formula(CompilerResults results, List<SensorVariable> sensorsUsed, SensorVariable sensorAppliedTo)
        {
            CompilerResults = results;
            SensorsUsed = sensorsUsed;
            SensorAppliedTo = sensorAppliedTo;
        }

        public CompilerResults CompilerResults { get; private set; }

        public List<SensorVariable> SensorsUsed { get; private set; }

        public SensorVariable SensorAppliedTo { get; private set; }

        public bool IsValid
        {
            get { return CompilerResults != null && CompilerResults.Errors.Count == 0 && CompilerResults.CompiledAssembly != null; }
        }
    }
}
