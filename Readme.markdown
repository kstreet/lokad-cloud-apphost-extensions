Lokad.Cloud AppHost Extensions
==============================

Extensions that can be used as starting point to connect your application to Lokad.Cloud.AppHost.

File Deployments
----------------

Provides a simple deployment reader for deployments stored in the local file system.

Be aware that once loaded, a deployment should never change (deletion is ok) without changing its name.
Hence consider appending an incrementing number to the deployment folder (e.g. "myDeployment-1") and increase
that number whenever any file inside of that folder is changed. This is a tradeoff to allow this deployment
extension to remain very simple.

File Structure:

  * **currentdeployment.txt**  
    Simple text file containing only one line with the name of the folder of the current deployment, e.g. "deploymentName-v3"

  * **deploymentName-v3/**  
    Directory containing the deployment named "deploymentName-v3".

  * **deploymentName-v3/MyCell/**  
    Directory of the cell named "MyCell" of the deployment "deploymentName-v3". This directory directly contains all the assemblies and symbols (.dll, .pdb), a text file "entrypoint.txt" and optionally a settings file "settings.xml".

  * **deploymentName-v3/MyCell/entrypoint.txt**  
    Simple text file containing only one line with the fully qualified name of the entry point of that cell. The entry point is a class implementing the *IApplicationEntryPoint* interface and should be in one of the assemblies in the same folder. A fully qualified name is of the format "Namespace.TypeName, AssemblyName" (without the .dll extension).

  * **deploymentName-v3/MyCell/settings.xml**  
    Optional. This is an xml file containing arbitrary settings and is automatically passed to your entry point.

  * **deploymentName-v2/AnotherCell/**  
    Directory of another cell of this deployment, named "AnotherCell".

  * **otherDeploymentName/**:  
    Directory of another deployment, named "otherDeploymentName"
