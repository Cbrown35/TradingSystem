// Health Checks UI JavaScript

class HealthCheckUI {
    constructor() {
        this.refreshInterval = 30000; // 30 seconds
        this.lastStatus = new Map();
        this.charts = new Map();
        this.init();
    }

    init() {
        this.setupAutoRefresh();
        this.setupTimestamps();
        this.setupCopyFunctionality();
        this.setupStatusObservers();
        this.setupMetricsCharts();
        this.setupNotifications();
        this.setupKeyboardShortcuts();
    }

    setupAutoRefresh() {
        let countdown = this.refreshInterval / 1000;
        const countdownElement = document.querySelector('.refresh-countdown');
        
        setInterval(() => {
            countdown--;
            if (countdownElement) {
                countdownElement.textContent = `Refreshing in ${countdown}s`;
            }
            if (countdown <= 0) {
                this.refreshHealthChecks();
                countdown = this.refreshInterval / 1000;
            }
        }, 1000);

        // Manual refresh button
        document.querySelector('.refresh-button')?.addEventListener('click', () => {
            this.refreshHealthChecks();
            countdown = this.refreshInterval / 1000;
        });
    }

    async refreshHealthChecks() {
        try {
            const response = await fetch(window.location.href);
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            // Update health check cards
            document.querySelector('.health-checks').innerHTML = 
                doc.querySelector('.health-checks').innerHTML;
            
            // Update timestamp
            document.querySelector('.timestamp').textContent = 
                doc.querySelector('.timestamp').textContent;
            
            this.setupMetricsCharts();
            this.notifyStatusChanges();
        } catch (error) {
            console.error('Failed to refresh health checks:', error);
            this.showNotification('Error refreshing health checks', 'error');
        }
    }

    setupTimestamps() {
        const formatter = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });
        
        document.querySelectorAll('.timestamp').forEach(element => {
            const date = new Date(element.textContent);
            element.title = element.textContent;
            
            // Update relative time every minute
            setInterval(() => {
                const seconds = Math.round((date - new Date()) / 1000);
                element.textContent = formatter.format(seconds, 'seconds');
            }, 60000);
        });
    }

    setupCopyFunctionality() {
        document.querySelectorAll('.copyable').forEach(element => {
            element.addEventListener('click', async () => {
                try {
                    await navigator.clipboard.writeText(element.textContent);
                    this.showNotification('Copied to clipboard!', 'success');
                } catch (error) {
                    console.error('Failed to copy:', error);
                    this.showNotification('Failed to copy to clipboard', 'error');
                }
            });
        });
    }

    setupStatusObservers() {
        document.querySelectorAll('.health-check-card').forEach(card => {
            const statusBadge = card.querySelector('.status-badge');
            const name = card.querySelector('h3').textContent;
            
            const observer = new MutationObserver(mutations => {
                mutations.forEach(mutation => {
                    if (mutation.type === 'characterData' || mutation.type === 'childList') {
                        const newStatus = statusBadge.textContent;
                        if (this.lastStatus.get(name) !== newStatus) {
                            this.animateStatusChange(card, newStatus);
                            this.lastStatus.set(name, newStatus);
                        }
                    }
                });
            });
            
            observer.observe(statusBadge, { 
                characterData: true, 
                childList: true, 
                subtree: true 
            });
            
            this.lastStatus.set(name, statusBadge.textContent);
        });
    }

    setupMetricsCharts() {
        document.querySelectorAll('.metrics-chart').forEach(container => {
            const canvas = container.querySelector('canvas');
            const data = JSON.parse(container.dataset.metrics);
            
            if (this.charts.has(canvas.id)) {
                this.charts.get(canvas.id).destroy();
            }
            
            this.charts.set(canvas.id, this.createChart(canvas, data));
        });
    }

    createChart(canvas, data) {
        return new Chart(canvas, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [{
                    label: data.label,
                    data: data.values,
                    borderColor: this.getStatusColor(data.status),
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 500
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    setupNotifications() {
        if ('Notification' in window) {
            Notification.requestPermission();
        }
    }

    showNotification(message, type = 'info') {
        // In-page notification
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.classList.add('fade-out');
            setTimeout(() => notification.remove(), 500);
        }, 3000);

        // Browser notification
        if (Notification.permission === 'granted' && type === 'error') {
            new Notification('Trading System Health Check', {
                body: message,
                icon: '/favicon.svg'
            });
        }
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', event => {
            if (event.key === 'r' && (event.ctrlKey || event.metaKey)) {
                event.preventDefault();
                this.refreshHealthChecks();
            }
        });
    }

    animateStatusChange(card, newStatus) {
        card.classList.add('status-changed');
        card.classList.remove('status-healthy', 'status-degraded', 'status-unhealthy');
        card.classList.add(`status-${newStatus.toLowerCase()}`);
        
        setTimeout(() => card.classList.remove('status-changed'), 1000);
        
        this.showNotification(
            `${card.querySelector('h3').textContent} status changed to ${newStatus}`,
            newStatus.toLowerCase()
        );
    }

    getStatusColor(status) {
        const colors = {
            Healthy: '#28a745',
            Degraded: '#ffc107',
            Unhealthy: '#dc3545'
        };
        return colors[status] || colors.Healthy;
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.healthCheckUI = new HealthCheckUI();
});
