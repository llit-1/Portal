// CSS стили модального окна
const modalStyle = `
    position: fixed;
    z-index: 100;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.4);
    display: none;
`;

// Создаем div элемент для модального окна
const modalDiv = document.createElement('div');
modalDiv.style = modalStyle;
modalDiv.classList.add('closeModal')
document.body.appendChild(modalDiv);

// CSS стили контента модального окна
const modalContentStyle = `
    background-color: #fefefe;
    margin: 10% auto;
    padding: 20px;
    border: 1px solid #888;
    width: 80%;
    max-width: 600px;
`;

// Создаем div элемент для контента модального окна
const modalContentDiv = document.createElement('div');
modalContentDiv.style = modalContentStyle;
modalDiv.appendChild(modalContentDiv);

// Создаем кнопку закрытия модального окна
const closeBtn = document.createElement('span');
closeBtn.textContent = '×';
closeBtn.style = `
    float: right;
    font-size: 24px;
    font-weight: bold;
    cursor: pointer;
`;

// Добавляем обработчик события для закрытия модального окна
closeBtn.addEventListener('click', () => {
    modalDiv.style.display = 'none';
});

// Создаем заголовок модального окна
const modalHeader = document.createElement('h2');
modalHeader.style = `    
    border-bottom: 1px solid #e5e5e5;
    padding-bottom: 10px
;`;
modalContentDiv.appendChild(closeBtn);
modalContentDiv.appendChild(modalHeader);

// Создаем тело модального окна
const modalBody = document.createElement('div');
modalBody.style = `
    padding-top: 15px;
`;
modalContentDiv.appendChild(modalBody);


// Обработчик события для закрытия модального окна по щелчку вне его
window.addEventListener('click', (event) => {
    if (event.target === modalDiv) {
        modalDiv.style.display = 'none';
    }
});

// Функция для открытия модального окна с сообщением
function openModal(header, body) {
    modalHeader.textContent = header;
    modalBody.innerHTML = body.replace(/\n/g, '<br>');
    modalDiv.style.display = 'block';
}

// 
function openModalQuestion(header, button) {
    modalHeader.textContent = header;
    modalDiv.style.display = 'block';
    modalBody.innerHTML = `<div class='quizModal'> <input type='date' class='quizInput'><button class='btn btn-sm btn-danger' onClick='getDateFromModal()'>${button}</button></div>`;
}

function openModalQuestionOpen(header, button) {
    modalHeader.textContent = header;
    modalDiv.style.display = 'block';
    modalBody.innerHTML = `<div class='quizModal'><button class='btn btn-sm btn-danger' onClick='clearCloseDate()'>${button}</button></div>`;
}

function openModalYesNo(header, buttonYes, buttonNo) {
    modalHeader.textContent = header;
    modalDiv.style.display = 'block';
    modalBody.innerHTML = `<div class='quizModal'><button class='btn btn-sm btn-danger' onClick='YesOrNoModal()'>${buttonYes}</button><button class='btn btn-sm btn-success' onClick='closeModal()'>${buttonNo}</button></div>`;
}

// Закрыть модальное окно
function closeModal() {
    const modal = document.querySelector('.closeModal');
    modal.click();
}