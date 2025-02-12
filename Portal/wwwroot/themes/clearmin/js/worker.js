let remain = 0;

self.onmessage = function (e) {
    remain = e.data; // получаем оставшееся время
    tick();
};

function tick() {
    if (remain > 0) {
        remain--;
        self.postMessage(remain);
        setTimeout(tick, 1000);
    } else {
        self.postMessage("logout");
    }
}
