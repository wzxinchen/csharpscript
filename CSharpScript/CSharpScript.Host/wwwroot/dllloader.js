console.log(window.dotnetLoadedEvent);
const currentScript =import.meta.url;
console.dir(currentScript);
window.addEventListener('dotnet_loaded', async () => {
    console.log("fired");
    console.log(currentScript);
    const assemblyName = new URLSearchParams(
        currentScript.split('?')[1]).get("name");
    console.log(assemblyName);
    const exports = await window.getAssemblyExports(assemblyName);
    console.log(exports);
    exports.Test.T();
})