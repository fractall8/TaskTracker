window.faqChat = {
    scrollToBottom: function (element, instant) {
        if (!element) {
            return;
        }

        const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        element.scrollTo({
            top: element.scrollHeight,
            behavior: (instant || reduceMotion) ? 'auto' : 'smooth'
        });
    }
};
