import apiClient from './client';

export interface ReviewResponse {
  id: number;
  applicationId: number;
  reviewerId: number;
  reviewerName: string;
  reviewerRole: string;
  recommendation: string;
  comments: string;
  reviewedAt: string;
}

export interface CreateReviewRequest {
  recommendation: string;
  comments: string;
}

export interface UpdateReviewRequest {
  recommendation: string;
  comments: string;
}

export const reviewsApi = {
  getReviewById: async (id: number): Promise<ReviewResponse> => {
    const response = await apiClient.get<ReviewResponse>(`/reviews/${id}`);
    return response.data;
  },

  getReviewsByApplicationId: async (applicationId: number): Promise<ReviewResponse[]> => {
    const response = await apiClient.get<ReviewResponse[]>(`/reviews/application/${applicationId}`);
    return response.data;
  },

  createReview: async (applicationId: number, data: CreateReviewRequest): Promise<ReviewResponse> => {
    const response = await apiClient.post<ReviewResponse>(`/reviews/application/${applicationId}`, data);
    return response.data;
  },

  updateReview: async (id: number, data: UpdateReviewRequest): Promise<ReviewResponse> => {
    const response = await apiClient.put<ReviewResponse>(`/reviews/${id}`, data);
    return response.data;
  },
};
