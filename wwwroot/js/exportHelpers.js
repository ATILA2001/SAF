// Descarga un archivo generado en el servidor recibiéndolo como DotNetStreamReference:
// Blazor lo transfiere en chunks por SignalR, sin base64 (ni +33% en la red, ni
// decodificación atob en el hilo de UI del navegador).
window.downloadFileFromStream = async function (fileName, contentStreamReference, mimeType) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
